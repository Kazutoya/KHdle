using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KingdomHeartsDle.Models;

namespace KingdomHeartsDle.Services;

public class GameService
{
    private readonly Dictionary<string, GameMode> _modes = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeZoneInfo _timeZone;
    private readonly I18n _i18n;

    public GameService(IWebHostEnvironment env, I18n i18n)
    {
        _i18n = i18n;
        _timeZone = ResolveTimeZone();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        if (!Directory.Exists(dataDir))
        {
            dataDir = Path.Combine(env.ContentRootPath, "Data");
        }

        foreach (var file in Directory.GetFiles(dataDir, "*.json"))
        {
            var mode = JsonSerializer.Deserialize<GameMode>(File.ReadAllText(file), options)
                       ?? throw new InvalidDataException($"Fichier vide ou invalide : {file}");
            Validate(mode, file);
            _modes[mode.Id] = mode;
        }
    }

    public IReadOnlyList<GameMode> Modes => _modes.Values.OrderBy(m => m.Order).ToList();

    public GameMode? GetMode(string? id) =>
        id is not null && _modes.TryGetValue(id, out var m) ? m : null;

    /// <summary>Date du jour à Paris : la nouvelle énigme tombe à minuit, heure de Paris.</summary>
    public DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _timeZone));

    /// <summary>Réponse du jour, déterministe (même réponse pour tout le monde, calculée côté serveur).</summary>
    public Entry GetTarget(GameMode mode, DateOnly date)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"khdle|{mode.Id}|{date:yyyy-MM-dd}"));
        var n = BitConverter.ToUInt32(hash, 0);
        return mode.Entries[(int)(n % (uint)mode.Entries.Count)];
    }

    public Entry? FindEntry(GameMode mode, string name) =>
        mode.Entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Cell> Compare(GameMode mode, Entry guess, Entry target)
    {
        var cells = new List<Cell>(mode.Columns.Count);
        foreach (var col in mode.Columns)
        {
            var g = guess.Values[col.Key];
            var t = target.Values[col.Key];

            switch (col.Kind)
            {
                case ColumnKind.Number:
                    if (int.TryParse(g, out var gn) && int.TryParse(t, out var tn) && gn != tn)
                        cells.Add(new Cell(g, CellState.Wrong, tn > gn ? 1 : -1));
                    else
                        cells.Add(new Cell(g, g == t ? CellState.Correct : CellState.Wrong));
                    break;

                case ColumnKind.List:
                    var gs = Split(g);
                    var ts = Split(t);
                    var state = gs.SetEquals(ts) ? CellState.Correct
                              : gs.Overlaps(ts) ? CellState.Partial
                              : CellState.Wrong;
                    cells.Add(new Cell(string.Join(", ", gs.Select(_i18n.Data)), state));
                    break;

                default:
                    cells.Add(new Cell(_i18n.Data(g), g.Equals(t, StringComparison.OrdinalIgnoreCase)
                        ? CellState.Correct : CellState.Wrong));
                    break;
            }
        }
        return cells;
    }

    /// <summary>Suggestions d'autocomplétion, insensibles à la casse et aux accents.</summary>
    public List<Entry> Search(GameMode mode, string query, IEnumerable<string> alreadyGuessed, int max = 8)
    {
        var q = Normalize(query);
        if (q.Length == 0) return new();

        var excluded = new HashSet<string>(alreadyGuessed, StringComparer.OrdinalIgnoreCase);
        return mode.Entries
            .Where(e => !excluded.Contains(e.Name))
            .Select(e => (Entry: e, Name: Normalize(_i18n.Data(e.Name))))
            .Where(x => x.Name.Contains(q))
            .OrderByDescending(x => x.Name.StartsWith(q))
            .ThenBy(x => _i18n.Data(x.Entry.Name), StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(x => x.Entry)
            .ToList();
    }

    private static HashSet<string> Split(string value) =>
        value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string s)
    {
        var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString();
    }

    private static void Validate(GameMode mode, string file)
    {
        if (string.IsNullOrWhiteSpace(mode.Id) || mode.Entries.Count == 0)
            throw new InvalidDataException($"Mode sans id ou sans entrées : {file}");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in mode.Entries)
        {
            if (!seen.Add(entry.Name))
                throw new InvalidDataException($"[{mode.Id}] doublon : {entry.Name}");

            foreach (var col in mode.Columns)
            {
                if (!entry.Values.TryGetValue(col.Key, out var v) || string.IsNullOrWhiteSpace(v))
                    throw new InvalidDataException($"[{mode.Id}] « {entry.Name} » n'a pas de valeur pour « {col.Key} »");
                if (col.Kind == ColumnKind.Number && !int.TryParse(v, out _))
                    throw new InvalidDataException($"[{mode.Id}] « {entry.Name} » : « {col.Key} » doit être un nombre");
            }
        }
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "Europe/Paris", "Romance Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
