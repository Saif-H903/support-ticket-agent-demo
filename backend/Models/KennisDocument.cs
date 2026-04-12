namespace Backend.Models;

public sealed class KennisDocument
{
    public string Id { get; set; } = string.Empty;
    public string Titel { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public string Inhoud { get; set; } = string.Empty;
    public string[] Trefwoorden { get; set; } = [];
}
