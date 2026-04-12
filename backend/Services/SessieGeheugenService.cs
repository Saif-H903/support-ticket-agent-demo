using System.Collections.Concurrent;
using Backend.Models;

namespace Backend.Services;

public sealed class SessieGeheugenService
{
    private const int MaxAantalBerichten = 5;
    private readonly ConcurrentDictionary<string, List<AuditLog>> _perSessie = new();

    public string MaakSamenvatting(string sessieId)
    {
        if (!_perSessie.TryGetValue(sessieId, out var logs) || logs.Count == 0)
        {
            return "Geen eerdere interacties in deze sessie.";
        }

        return string.Join(Environment.NewLine, logs.TakeLast(3).Select(log =>
            $"- Vraag: {log.Vraag} | Actie: {log.EindActie} | Bron: {log.BronTitel}"));
    }

    public bool VerwachtBestelnummer(string sessieId)
    {
        if (!_perSessie.TryGetValue(sessieId, out var logs))
        {
            return false;
        }

        lock (logs)
        {
            var laatsteLog = logs.LastOrDefault();
            return laatsteLog?.EindActie == "bestelnummer gevraagd";
        }
    }

    public void VoegToe(AuditLog log)
    {
        var logs = _perSessie.GetOrAdd(log.SessieId, _ => []);

        lock (logs)
        {
            logs.Add(log);
            if (logs.Count > MaxAantalBerichten)
            {
                logs.RemoveRange(0, logs.Count - MaxAantalBerichten);
            }
        }
    }
}