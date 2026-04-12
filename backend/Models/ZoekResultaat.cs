namespace Backend.Models;

public sealed class ZoekResultaat
{
    public KennisDocument Document { get; init; } = new();
    public int TrefwoordHits { get; init; }
    public int TekstHits { get; init; }
    public double Score { get; init; }
}
