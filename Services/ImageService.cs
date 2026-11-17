using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KingdomHeartsDle.Models;
using Microsoft.Extensions.FileProviders;

namespace KingdomHeartsDle.Services;

/// <summary>
/// Retrouve l'image d'une entrée par convention de nom, sans rien déclarer dans les JSON :
///   wwwroot/images/{id du mode}/{slug du nom}.webp   (ou .png / .jpg / .jpeg)
/// Exemple : « Étoile Angélique » dans le mode ennemi → wwwroot/images/ennemi/etoile-angelique.webp
/// Si le fichier n'existe pas, une image par défaut est affichée. Une entrée peut aussi
/// imposer son fichier avec le champ JSON optionnel "image".
/// </summary>
public class ImageService
{
    public const string Placeholder = "images/placeholder.svg";
    private static readonly string[] Extensions = { ".webp", ".png", ".jpg", ".jpeg" };

    private readonly IFileProvider _web;
    private readonly GameService _game;
    private readonly ILogger<ImageService> _log;

    public ImageService(IWebHostEnvironment env, GameService game, ILogger<ImageService> log)
    {
        _web = env.WebRootFileProvider;
        _game = game;
        _log = log;
    }

    /// <summary>Nom de fichier sans extension : minuscules, sans accents, tout le reste devient « - ».</summary>
    /// <remarks>Doit rester identique à slugify() dans tools/prepare_images.py.</remarks>
    public static string Slug(string name)
    {
        var s = name.Trim().ToLowerInvariant().Replace("œ", "oe").Replace("æ", "ae");
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return Regex.Replace(sb.ToString(), "[^a-z0-9]+", "-").Trim('-');
    }

    /// <summary>URL relative de l'image de l'entrée (ou de l'image par défaut).</summary>
    public string Url(GameMode mode, Entry entry) => Find(mode, entry) ?? Placeholder;

    public bool Has(GameMode mode, Entry entry) => Find(mode, entry) is not null;

    private string? Find(GameMode mode, Entry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Image))
        {
            var forced = $"images/{mode.Id}/{entry.Image}";
            return _web.GetFileInfo(forced).Exists ? forced : null;
        }

        var slug = Slug(entry.Name);
        foreach (var ext in Extensions)
        {
            var path = $"images/{mode.Id}/{slug}{ext}";
            if (_web.GetFileInfo(path).Exists) return path;
        }
        return null;
    }

    /// <summary>Fichier attendu pour une entrée, affiché dans le rapport.</summary>
    public static string Expected(GameMode mode, Entry entry) =>
        !string.IsNullOrWhiteSpace(entry.Image)
            ? $"images/{mode.Id}/{entry.Image}"
            : $"images/{mode.Id}/{Slug(entry.Name)}.webp";

    public object Report() => _game.Modes.Select(m =>
    {
        var missing = m.Entries.Where(e => !Has(m, e)).Select(e => new { e.Name, expected = Expected(m, e) }).ToList();
        return new { mode = m.Id, total = m.Entries.Count, found = m.Entries.Count - missing.Count, missing };
    }).ToList();

    /// <summary>Résumé au démarrage + alerte si deux noms donnent le même fichier.</summary>
    public void LogSummary()
    {
        foreach (var m in _game.Modes)
        {
            var found = m.Entries.Count(e => Has(m, e));
            _log.LogInformation("Images [{Mode}] : {Found}/{Total} trouvées", m.Id, found, m.Entries.Count);

            foreach (var group in m.Entries.Where(e => string.IsNullOrWhiteSpace(e.Image))
                                           .GroupBy(e => Slug(e.Name)).Where(g => g.Count() > 1))
            {
                _log.LogWarning("Images [{Mode}] : « {Names} » donnent le même fichier « {Slug} ». Ajoute un champ \"image\" à l'une des entrées.",
                    m.Id, string.Join(" » et « ", group.Select(e => e.Name)), group.Key);
            }
        }
    }
}
