namespace Backend.Models;

public sealed class BestelStatusResultaat
{
    public string BestellingId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Verwachting { get; set; } = string.Empty;
    public DateTime LaatstBijgewerktUtc { get; set; } = DateTime.UtcNow;
}
