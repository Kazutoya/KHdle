namespace KingdomHeartsDle.Models;

/// <summary>
/// Progression d'un mode pour une journée, enregistrée dans le navigateur (stockage protégé).
/// Partagée par la page de jeu (qui l'écrit) et l'accueil (qui la lit pour afficher « Réussi »).
/// </summary>
public sealed record SavedState(List<string> Guesses)
{
    /// <summary>Clé de stockage : un enregistrement par mode et par jour (heure de Paris).</summary>
    public static string Key(string modeId, DateOnly date) => $"khdle:{modeId}:{date:yyyy-MM-dd}";
}