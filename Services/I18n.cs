using System.Globalization;
using System.Text.Json;

namespace KingdomHeartsDle.Services;

/// <summary>
/// Traductions français / anglais.
/// - Interface : dictionnaire de clés ci-dessous.
/// - Contenu du jeu : Data/i18n/translations.en.json associe un texte français à sa version anglaise
///   (un texte absent du fichier est affiché tel quel dans les deux langues).
/// La langue vient de la culture de la requête (cookie, sinon en-tête Accept-Language).
/// </summary>
public class I18n
{
    public static readonly string[] SupportedLanguages = { "fr", "en" };

    private readonly Dictionary<string, string> _dataEn;

    public I18n(IWebHostEnvironment env)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Data");
        if (!Directory.Exists(dir)) dir = Path.Combine(env.ContentRootPath, "Data");

        var path = Path.Combine(dir, "i18n", "translations.en.json");
        _dataEn = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new()
            : new();
    }

    public bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
    public string Lang => IsEnglish ? "en" : "fr";

    /// <summary>Texte d'interface dans la langue courante.</summary>
    public string this[string key] =>
        Ui.TryGetValue(key, out var t) ? (IsEnglish ? t.En : t.Fr) : key;

    public string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, this[key], args);

    /// <summary>Texte de contenu (nom, valeur, libellé de colonne…) dans la langue courante.</summary>
    public string Data(string fr) =>
        IsEnglish && _dataEn.TryGetValue(fr, out var en) ? en : fr;

    public string Guesses(int n) => n > 1 ? this["guesses"] : this["guess"];

    private static readonly Dictionary<string, (string Fr, string En)> Ui = new()
    {
        ["home.title"] = ("KHdle – Le jeu quotidien Kingdom Hearts", "KHdle – The daily Kingdom Hearts game"),
        ["home.intro"] = ("Chaque jour, une nouvelle énigme sur l'univers de Kingdom Hearts. Choisis un mode et devine la réponse grâce aux indices !",
                          "Every day, a new puzzle about the Kingdom Hearts universe. Pick a mode and guess the answer from the clues!"),
        ["home.possibilities"] = ("{0} possibilités", "{0} possibilities"),
        ["home.how"] = ("Comment jouer ?", "How to play"),
        ["legend.correct"] = ("Vert : l'indice est exact", "Green: the clue is correct"),
        ["legend.partial"] = ("Orange : l'indice est partiellement exact", "Orange: the clue is partially correct"),
        ["legend.wrong"] = ("Rouge : l'indice est faux", "Red: the clue is wrong"),
        ["legend.arrow"] = ("La réponse est plus récente / plus ancienne", "The answer is more recent / older"),

        ["layout.nav"] = ("Modes de jeu", "Game modes"),
        ["layout.language"] = ("Langue", "Language"),
        ["layout.footer"] = ("Site de fan non officiel. Kingdom Hearts, Disney et Square Enix sont des marques de leurs propriétaires respectifs. Une nouvelle énigme chaque jour à minuit (heure de Paris).",
                             "Unofficial fan site. Kingdom Hearts, Disney and Square Enix are trademarks of their respective owners. A new puzzle every day at midnight (Paris time)."),
        ["layout.error"] = ("Une erreur est survenue.", "An error has occurred."),
        ["layout.reload"] = ("Recharger", "Reload"),

        ["meta.description"] = ("KHdle : devine chaque jour un personnage, un boss, un monde, un Keyblade ou un ennemi de Kingdom Hearts.",
                                "KHdle: guess a Kingdom Hearts character, boss, world, Keyblade or enemy every day."),

        ["play.notfound"] = ("Mode introuvable", "Mode not found"),
        ["play.back"] = ("Retour à l'accueil", "Back to home"),
        ["play.win"] = ("Bravo !", "Well done!"),
        ["play.answerWas"] = ("La réponse du jour était", "Today's answer was"),
        ["play.foundIn"] = ("Trouvé en", "Found in"),
        ["play.copy"] = ("Copier le résultat", "Copy result"),
        ["play.copied"] = ("Copié ✓", "Copied ✓"),
        ["play.next"] = ("Prochaine énigme à minuit (heure de Paris). Essaie un autre mode !",
                         "Next puzzle at midnight (Paris time). Try another mode!"),
        ["play.noGuess"] = ("Aucun essai pour l'instant", "No guesses yet"),
        ["play.colName"] = ("Nom", "Name"),
        ["play.newer"] = ("Plus récent", "More recent"),
        ["play.older"] = ("Plus ancien", "Older"),
        ["guess"] = ("essai", "guess"),
        ["guesses"] = ("essais", "guesses"),
    };
}
