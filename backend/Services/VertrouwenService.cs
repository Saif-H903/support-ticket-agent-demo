using Backend.Models;

namespace Backend.Services;

public sealed class VertrouwenService
{
    private const double Drempel = 0.75;

    public (double Score, string Reden, bool Escaleren) Bereken(
        ZoekResultaat? zoekResultaat,
        bool toolGebruikt,
        bool heeftToolResultaat,
        bool toolResultaatOnzeker,
        string vraag)
    {
        var score = 0.0;
        var redenen = new List<string>();

        if (zoekResultaat is not null)
        {
            score += 0.62;
            score += Math.Min(0.18, zoekResultaat.TrefwoordHits * 0.08);
            score += Math.Min(0.07, zoekResultaat.TekstHits * 0.02);
            redenen.Add($"kennisbankmatch '{zoekResultaat.Document.Titel}' gevonden");
        }

        if (toolGebruikt)
        {
            score += 0.05;
            redenen.Add("toolroute geactiveerd");
        }

        if (heeftToolResultaat)
        {
            score += 0.75;
            redenen.Add("tool gaf concrete bestelstatus terug");
        }

        if (toolResultaatOnzeker)
        {
            score -= 0.45;
            redenen.Add("toolresultaat is onzeker en vereist handmatige controle");
        }

        if (vraag.Trim().Length >= 10)
        {
            score += 0.05;
            redenen.Add("vraag is specifiek genoeg");
        }

        if (zoekResultaat is null && !heeftToolResultaat)
        {
            redenen.Add("geen betrouwbare bron of tooldata gevonden");
        }

        score = Math.Round(Math.Clamp(score, 0, 1), 2);
        var escaleren = toolResultaatOnzeker || score < Drempel;

        redenen.Add(escaleren
            ? $"onder vertrouwensdrempel van {Drempel:0.00}"
            : $"boven vertrouwensdrempel van {Drempel:0.00}");

        if (toolResultaatOnzeker)
        {
            redenen.Add("handmatige beoordeling verplicht door onzeker toolresultaat");
        }

        return (score, string.Join("; ", redenen), escaleren);
    }
}
