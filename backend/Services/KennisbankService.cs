using System.Text.Json;
using Backend.Models;

namespace Backend.Services;

public sealed class KennisbankService
{
    private readonly string _pad;
    private readonly JsonSerializerOptions _jsonOpties = new(JsonSerializerDefaults.Web);

    public KennisbankService(IHostEnvironment omgeving)
    {
        _pad = Path.Combine(omgeving.ContentRootPath, "Data", "knowledge-base.json");
    }

    public async Task<IReadOnlyList<KennisDocument>> HaalAllesOpAsync()
    {
        if (!File.Exists(_pad))
        {
            return [];
        }

        await using var stream = File.OpenRead(_pad);
        return await JsonSerializer.DeserializeAsync<List<KennisDocument>>(stream, _jsonOpties) ?? [];
    }
}
