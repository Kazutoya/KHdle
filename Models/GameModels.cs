namespace KingdomHeartsDle.Models;

/// <summary>Type de comparaison appliqué à une colonne d'indices.</summary>
public enum ColumnKind
{
    /// <summary>Valeur unique : identique ou différente.</summary>
    Text,
    /// <summary>Plusieurs valeurs séparées par « | » : identique, partielle ou différente.</summary>
    List,
    /// <summary>Valeur numérique : identique ou plus haut / plus bas (flèche).</summary>
    Number
}

public class Column
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public ColumnKind Kind { get; set; } = ColumnKind.Text;
}

public class Entry
{
    public string Name { get; set; } = "";
    /// <summary>Valeurs par clé de colonne (les nombres sont stockés en texte).</summary>
    public Dictionary<string, string> Values { get; set; } = new();
}

public class GameMode
{
    public string Id { get; set; } = "";
    public int Order { get; set; }
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Description { get; set; } = "";
    public string Placeholder { get; set; } = "Tape un nom…";
    public List<Column> Columns { get; set; } = new();
    public List<Entry> Entries { get; set; } = new();
}

public enum CellState { Correct, Partial, Wrong }

/// <summary>Résultat de la comparaison d'une colonne. Arrow : +1 = la réponse est plus haute, -1 = plus basse.</summary>
public record Cell(string Display, CellState State, int Arrow = 0);
