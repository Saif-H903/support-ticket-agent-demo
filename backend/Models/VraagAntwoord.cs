namespace Backend.Models;

public sealed class VraagAntwoord
{
    public string Antwoord { get; set; } = string.Empty;
    public string AntwoordBron { get; set; } = string.Empty;
    public double Vertrouwen { get; set; }
    public bool Escaleren { get; set; }
    public string BronTitel { get; set; } = string.Empty;
    public string BronFragment { get; set; } = string.Empty;
    public string ToolGebruikt { get; set; } = string.Empty;
    public string ToolUitvoer { get; set; } = string.Empty;
    public string BesluitReden { get; set; } = string.Empty;
}
