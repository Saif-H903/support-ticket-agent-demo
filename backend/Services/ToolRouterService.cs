using System.Text.RegularExpressions;

namespace Backend.Services;

public sealed partial class ToolRouterService
{
    public bool MoetBestelStatusToolGebruiken(string vraag, out string bestellingId)
    {
        bestellingId = string.Empty;

        var match = BestellingRegex().Match(vraag);
        if (!HeeftBestelStatusIntentie(vraag) || !match.Success)
        {
            return false;
        }

        bestellingId = match.Value;
        return true;
    }

    public bool HeeftBestelnummer(string vraag, out string bestellingId)
    {
        var match = BestellingRegex().Match(vraag);
        bestellingId = match.Success ? match.Value : string.Empty;
        return match.Success;
    }

    public bool MistBestellingNummerVoorLookup(string vraag)
    {
        return HeeftBestelStatusIntentie(vraag) && !BestellingRegex().IsMatch(vraag);
    }

    private static bool HeeftBestelStatusIntentie(string vraag)
    {
        var lager = vraag.ToLowerInvariant();
        var noemtBestelling =
            lager.Contains("bestelling") ||
            lager.Contains("order") ||
            lager.Contains("tracking") ||
            lager.Contains("pakket") ||
            lager.Contains("bestelnummer");

        var persoonlijkeBestelVraag =
            lager.Contains("mijn bestelling") ||
            lager.Contains("mijn order") ||
            lager.Contains("mijn pakket") ||
            lager.Contains("m'n bestelling") ||
            lager.Contains("m'n order") ||
            lager.Contains("m'n pakket") ||
            lager.Contains("bestelnummer");

        var wilLookup =
            lager.Contains("opzoeken") ||
            lager.Contains("status") ||
            lager.Contains("waar is") ||
            lager.Contains("waar blijft") ||
            lager.Contains("wanneer komt") ||
            lager.Contains("wanneer krijg") ||
            lager.Contains("niet ontvangen") ||
            lager.Contains("nog niet ontvangen") ||
            lager.Contains("niet aangekomen") ||
            lager.Contains("nog niet aangekomen") ||
            lager.Contains("vertraging") ||
            lager.Contains("track") ||
            lager.Contains("volgen") ||
            lager.Contains("bezorgd") ||
            lager.Contains("bezorgen") ||
            lager.Contains("levering") ||
            lager.Contains("bezorging");

        return noemtBestelling && (persoonlijkeBestelVraag || wilLookup);
    }

    [GeneratedRegex("\\b\\d{4,10}\\b")]
    private static partial Regex BestellingRegex();
}