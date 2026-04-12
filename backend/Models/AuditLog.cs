namespace Backend.Models;

public sealed class AuditLog
{
    public DateTime TijdstipUtc { get; set; } = DateTime.UtcNow;
    public string SessieId { get; set; } = string.Empty;
    public string Vraag { get; set; } = string.Empty;
    public string Antwoord { get; set; } = string.Empty;
    public string AntwoordBron { get; set; } = string.Empty;
    public string BronTitel { get; set; } = string.Empty;
    public double Vertrouwen { get; set; }
    public bool Escaleren { get; set; }
    public string ToolGebruikt { get; set; } = string.Empty;
    public string ToolUitvoer { get; set; } = string.Empty;
    public string BesluitReden { get; set; } = string.Empty;
    public string EindActie { get; set; } = string.Empty;
}
