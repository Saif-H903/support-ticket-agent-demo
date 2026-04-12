using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
public sealed class OndersteuningController : ControllerBase
{
    private readonly KennisbankService _kennisbankService;
    private readonly ZoekService _zoekService;
    private readonly ToolRouterService _toolRouterService;
    private readonly BestelStatusService _bestelStatusService;
    private readonly TaalmodelService _taalmodelService;
    private readonly VertrouwenService _vertrouwenService;
    private readonly AuditService _auditService;
    private readonly SessieGeheugenService _geheugenService;

    public OndersteuningController(
        KennisbankService kennisbankService,
        ZoekService zoekService,
        ToolRouterService toolRouterService,
        BestelStatusService bestelStatusService,
        TaalmodelService taalmodelService,
        VertrouwenService vertrouwenService,
        AuditService auditService,
        SessieGeheugenService geheugenService)
    {
        _kennisbankService = kennisbankService;
        _zoekService = zoekService;
        _toolRouterService = toolRouterService;
        _bestelStatusService = bestelStatusService;
        _taalmodelService = taalmodelService;
        _vertrouwenService = vertrouwenService;
        _auditService = auditService;
        _geheugenService = geheugenService;
    }

    [HttpPost("api/ondersteuning/vraag")]
    [HttpPost("api/support/ask")]
    public async Task<ActionResult<VraagAntwoord>> StelVraag([FromBody] VraagRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Vraag))
        {
            return BadRequest(new { fout = "Vul een supportvraag in." });
        }

        if (IsGespreksOpening(request.Vraag))
        {
            var taalmodelResultaat = await _taalmodelService.GenereerGespreksAntwoordAsync(
                request.Vraag,
                _geheugenService.MaakSamenvatting(request.SessieId));

            var gespreksResponse = new VraagAntwoord
            {
                Antwoord = taalmodelResultaat.Antwoord,
                AntwoordBron = taalmodelResultaat.AntwoordBron,
                Vertrouwen = 0.90,
                Escaleren = false,
                BronTitel = "Conversatie-intake",
                BronFragment = "Begroeting of algemene agentvraag; geen kennisbankfeit nodig.",
                BesluitReden = "gespreksopening herkend; agent vraagt waarmee hij kan helpen"
            };

            var gespreksAuditLog = new AuditLog
            {
                SessieId = request.SessieId,
                Vraag = request.Vraag,
                Antwoord = gespreksResponse.Antwoord,
                AntwoordBron = gespreksResponse.AntwoordBron,
                BronTitel = gespreksResponse.BronTitel,
                Vertrouwen = gespreksResponse.Vertrouwen,
                Escaleren = gespreksResponse.Escaleren,
                BesluitReden = gespreksResponse.BesluitReden,
                EindActie = "conversatie-intake"
            };

            await _auditService.VoegToeAsync(gespreksAuditLog);
            _geheugenService.VoegToe(gespreksAuditLog);

            return Ok(gespreksResponse);
        }

        if (_toolRouterService.MistBestellingNummerVoorLookup(request.Vraag))
        {
            var taalmodelResultaat = await _taalmodelService.GenereerBestelnummerVraagAsync(
                request.Vraag,
                _geheugenService.MaakSamenvatting(request.SessieId));

            var bestelnummerResponse = new VraagAntwoord
            {
                Antwoord = taalmodelResultaat.Antwoord,
                AntwoordBron = taalmodelResultaat.AntwoordBron,
                Vertrouwen = 0.90,
                Escaleren = false,
                BronTitel = "Bestelstatus-intake",
                BronFragment = "De gebruiker wil een bestelling opzoeken, maar er is nog geen bestelnummer gegeven.",
                ToolGebruikt = "bestelstatus",
                BesluitReden = "bestelstatus-intentie herkend; bestelnummer ontbreekt"
            };

            var bestelnummerAuditLog = new AuditLog
            {
                SessieId = request.SessieId,
                Vraag = request.Vraag,
                Antwoord = bestelnummerResponse.Antwoord,
                AntwoordBron = bestelnummerResponse.AntwoordBron,
                BronTitel = bestelnummerResponse.BronTitel,
                Vertrouwen = bestelnummerResponse.Vertrouwen,
                Escaleren = bestelnummerResponse.Escaleren,
                ToolGebruikt = bestelnummerResponse.ToolGebruikt,
                BesluitReden = bestelnummerResponse.BesluitReden,
                EindActie = "bestelnummer gevraagd"
            };

            await _auditService.VoegToeAsync(bestelnummerAuditLog);
            _geheugenService.VoegToe(bestelnummerAuditLog);

            return Ok(bestelnummerResponse);
        }

        var bestellingIdUitFollowUp = string.Empty;
        var isBestelnummerFollowUp =
            _geheugenService.VerwachtBestelnummer(request.SessieId) &&
            _toolRouterService.HeeftBestelnummer(request.Vraag, out bestellingIdUitFollowUp);

        var documenten = await _kennisbankService.HaalAllesOpAsync();
        var zoekResultaat = _zoekService.ZoekBesteMatch(request.Vraag, documenten);

        var toolGebruikt = string.Empty;
        var toolUitvoer = string.Empty;
        var heeftToolResultaat = false;
        var toolResultaatOnzeker = false;

        var heeftBestellingIdUitVraag = _toolRouterService.MoetBestelStatusToolGebruiken(request.Vraag, out var bestellingIdUitVraag);
        if (isBestelnummerFollowUp || heeftBestellingIdUitVraag)
        {
            var effectieveBestellingId = isBestelnummerFollowUp ? bestellingIdUitFollowUp : bestellingIdUitVraag;
            var status = _bestelStatusService.HaalStatusOp(effectieveBestellingId);
            toolResultaatOnzeker = ToolResultaatIsOnzeker(status);
            toolGebruikt = "bestelstatus";
            toolUitvoer = $"Bestelling {status.BestellingId}: {status.Status}, {status.Verwachting}";
            heeftToolResultaat = !toolResultaatOnzeker;
        }

        var (vertrouwen, reden, escaleren) = _vertrouwenService.Bereken(
            zoekResultaat,
            !string.IsNullOrWhiteSpace(toolGebruikt),
            heeftToolResultaat,
            toolResultaatOnzeker,
            request.Vraag);

        var antwoordBron = "fallback:confidence-threshold";
        var antwoord = "Ik heb niet genoeg betrouwbare informatie om dit veilig af te handelen. Ik zet dit ticket door naar een menselijke medewerker.";

        if (!escaleren)
        {
            var taalmodelResultaat = await _taalmodelService.GenereerAntwoordAsync(
                request.Vraag,
                zoekResultaat?.Document.Titel ?? (!string.IsNullOrWhiteSpace(toolUitvoer) ? "Bestelstatus-tool" : "Geen passende bron"),
                zoekResultaat?.Document.Inhoud ?? (!string.IsNullOrWhiteSpace(toolUitvoer) ? "Gebruik het toolresultaat als enige bron voor de bestelstatus." : "Geen kennisbankcontext beschikbaar."),
                toolUitvoer,
                _geheugenService.MaakSamenvatting(request.SessieId));

            antwoord = taalmodelResultaat.Antwoord;
            antwoordBron = taalmodelResultaat.AntwoordBron;
        }

        var eindActie = escaleren ? "geescaleerd naar human-in-the-loop" : "automatisch beantwoord";

        var response = new VraagAntwoord
        {
            Antwoord = antwoord,
            AntwoordBron = antwoordBron,
            Vertrouwen = vertrouwen,
            Escaleren = escaleren,
            BronTitel = zoekResultaat?.Document.Titel ?? (!string.IsNullOrWhiteSpace(toolUitvoer) ? "Bestelstatus-tool" : "Geen passende bron"),
            BronFragment = zoekResultaat?.Document.Inhoud ?? toolUitvoer,
            ToolGebruikt = toolGebruikt,
            ToolUitvoer = toolUitvoer,
            BesluitReden = reden
        };

        var auditLog = new AuditLog
        {
            SessieId = request.SessieId,
            Vraag = request.Vraag,
            Antwoord = response.Antwoord,
            AntwoordBron = response.AntwoordBron,
            BronTitel = response.BronTitel,
            Vertrouwen = response.Vertrouwen,
            Escaleren = response.Escaleren,
            ToolGebruikt = response.ToolGebruikt,
            ToolUitvoer = response.ToolUitvoer,
            BesluitReden = response.BesluitReden,
            EindActie = eindActie
        };

        await _auditService.VoegToeAsync(auditLog);
        _geheugenService.VoegToe(auditLog);

        return Ok(response);
    }

    private static bool IsGespreksOpening(string vraag)
    {
        var tekst = vraag.Trim().ToLowerInvariant();
        if (tekst.Length > 120)
        {
            return false;
        }

        if (tekst is "hallo" or "hoi" or "hey" or "hi" or "goedemorgen" or "goedemiddag" or "goedenavond" or "help")
        {
            return true;
        }

        return tekst.StartsWith("hallo ") ||
            tekst.StartsWith("hoi ") ||
            tekst.StartsWith("hey ") ||
            tekst.Contains("wat kun je") ||
            tekst.Contains("wat kan je") ||
            tekst.Contains("waarmee kun je") ||
            tekst.Contains("wie ben je");
    }

    private static bool ToolResultaatIsOnzeker(BestelStatusResultaat status)
    {
        var tekst = $"{status.Status} {status.Verwachting}";

        return tekst.Contains("onbekend", StringComparison.OrdinalIgnoreCase) ||
            tekst.Contains("laat een medewerker", StringComparison.OrdinalIgnoreCase) ||
            tekst.Contains("handmatige controle", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet("api/ondersteuning/logs")]
    [HttpGet("api/support/logs")]
    public async Task<ActionResult<IReadOnlyList<AuditLog>>> Logs()
    {
        var logs = await _auditService.HaalAllesOpAsync();
        return Ok(logs.OrderByDescending(log => log.TijdstipUtc));
    }
}
