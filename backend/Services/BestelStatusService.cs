using Backend.Models;

namespace Backend.Services;

public sealed class BestelStatusService
{
    private static readonly IReadOnlyDictionary<string, BestelStatusResultaat> DemoBestellingen =
        new Dictionary<string, BestelStatusResultaat>
        {
            ["12345"] = new()
            {
                BestellingId = "12345",
                Status = "verzonden",
                Verwachting = "verwacht binnen 2 werkdagen",
                LaatstBijgewerktUtc = DateTime.UtcNow.AddHours(-3)
            },
            ["98765"] = new()
            {
                BestellingId = "98765",
                Status = "in behandeling",
                Verwachting = "wordt nog verwerkt; opnieuw controleren als dit langer dan 3 werkdagen duurt",
                LaatstBijgewerktUtc = DateTime.UtcNow.AddHours(-8)
            }
        };

    public BestelStatusResultaat HaalStatusOp(string bestellingId)
    {
        if (DemoBestellingen.TryGetValue(bestellingId, out var resultaat))
        {
            return resultaat;
        }

        return new BestelStatusResultaat
        {
            BestellingId = bestellingId,
            Status = "onbekend in demo-tool",
            Verwachting = "laat een medewerker de bestelling controleren",
            LaatstBijgewerktUtc = DateTime.UtcNow
        };
    }
}
