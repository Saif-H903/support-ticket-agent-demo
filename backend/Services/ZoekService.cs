using System.Text.RegularExpressions;
using Backend.Models;

namespace Backend.Services;

public sealed partial class ZoekService
{
    public ZoekResultaat? ZoekBesteMatch(string vraag, IReadOnlyList<KennisDocument> documenten)
    {
        var woorden = NormaliseerWoorden(vraag);

        var beste = documenten
            .Select(document =>
            {
                var trefwoordHits = document.Trefwoorden.Count(trefwoord =>
                    vraag.Contains(trefwoord, StringComparison.OrdinalIgnoreCase));

                var tekstHits = NormaliseerWoorden(document.Inhoud)
                    .Distinct()
                    .Count(woorden.Contains);

                var score = trefwoordHits * 2 + tekstHits;

                return new ZoekResultaat
                {
                    Document = document,
                    TrefwoordHits = trefwoordHits,
                    TekstHits = tekstHits,
                    Score = score
                };
            })
            .OrderByDescending(resultaat => resultaat.Score)
            .FirstOrDefault();

        return beste is null || beste.Score <= 0 ? null : beste;
    }

    private static HashSet<string> NormaliseerWoorden(string tekst)
    {
        return WoordRegex()
            .Matches(tekst.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(woord => woord.Length > 2)
            .ToHashSet();
    }

    [GeneratedRegex("[\\p{L}\\p{N}]+")]
    private static partial Regex WoordRegex();
}
