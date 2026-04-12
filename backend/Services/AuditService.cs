using System.Text.Json;
using Backend.Models;

namespace Backend.Services;

public sealed class AuditService
{
    private readonly string _pad;
    private readonly JsonSerializerOptions _jsonOpties = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly SemaphoreSlim _slot = new(1, 1);

    public AuditService(IHostEnvironment omgeving)
    {
        var dataMap = Path.Combine(omgeving.ContentRootPath, "Data");
        Directory.CreateDirectory(dataMap);
        _pad = Path.Combine(dataMap, "logs.json");
    }

    public async Task VoegToeAsync(AuditLog log)
    {
        await _slot.WaitAsync();
        try
        {
            var logs = await HaalAllesOpZonderSlotAsync();
            logs.Add(log);

            var json = JsonSerializer.Serialize(logs, _jsonOpties);
            await File.WriteAllTextAsync(_pad, json);
        }
        finally
        {
            _slot.Release();
        }
    }

    public async Task<IReadOnlyList<AuditLog>> HaalAllesOpAsync()
    {
        await _slot.WaitAsync();
        try
        {
            return await HaalAllesOpZonderSlotAsync();
        }
        finally
        {
            _slot.Release();
        }
    }

    private async Task<List<AuditLog>> HaalAllesOpZonderSlotAsync()
    {
        if (!File.Exists(_pad))
        {
            return [];
        }

        var tekst = await File.ReadAllTextAsync(_pad);
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<AuditLog>>(tekst, _jsonOpties) ?? [];
    }
}
