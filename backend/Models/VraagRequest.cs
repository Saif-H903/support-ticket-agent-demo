namespace Backend.Models;

public sealed class VraagRequest
{
    public string Vraag { get; set; } = string.Empty;
    public string? Question
    {
        get => Vraag;
        set => Vraag = value ?? string.Empty;
    }

    public string SessieId { get; set; } = "demo-sessie-1";
    public string? SessionId
    {
        get => SessieId;
        set => SessieId = string.IsNullOrWhiteSpace(value) ? "demo-sessie-1" : value;
    }
}
