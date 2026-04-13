using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Backend.Services;

public sealed partial class TaalmodelService
{
    private const string SysteemPrompt = """
        Je bent een support-ticket-agent.
        Antwoord uitsluitend met de aangeleverde kennisbankcontext en toolresultaten.
        Verzin geen feiten.
        Als de informatie onvoldoende is, zeg dan dat het ticket naar een menselijke medewerker moet.
        Houd het antwoord kort, praktisch en in het Nederlands.
        Noem de brontitel natuurlijk als dat helpt.
        """;

    private const string GespreksSysteemPrompt = """
        Je bent een vriendelijke Nederlandse support-agent.
        Je mag de gebruiker begroeten, kort uitleggen waarmee je kunt helpen en een vervolgvraag stellen.
        Geef geen feitelijke order-, refund-, account- of factuurinformatie zonder kennisbankcontext of toolresultaten.
        Houd het antwoord kort, warm en praktisch.
        """;

    private readonly HttpClient _httpClient;
    private readonly ILogger<TaalmodelService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _provider;
    private readonly string _ollamaBaseUrl;
    private readonly string _ollamaModel;

    public TaalmodelService(HttpClient httpClient, IConfiguration configuratie, ILogger<TaalmodelService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _model = Environment.GetEnvironmentVariable("OPENAI_MODEL")
            ?? configuratie["OpenAI:Model"]
            ?? "gpt-4.1-mini";
        _provider = (Environment.GetEnvironmentVariable("AI_PROVIDER")
            ?? configuratie["AI:Provider"]
            ?? "openai").ToLowerInvariant();
        _ollamaBaseUrl = (Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")
            ?? configuratie["Ollama:BaseUrl"]
            ?? "http://localhost:11434").TrimEnd('/');
        _ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL")
            ?? configuratie["Ollama:Model"]
            ?? "qwen3:1.7b";
    }

    public async Task<(string Antwoord, string AntwoordBron)> GenereerAntwoordAsync(
        string vraag,
        string bronTitel,
        string bronInhoud,
        string toolUitvoer,
        string geheugenSamenvatting)
    {
        var prompt = MaakPrompt(vraag, bronTitel, bronInhoud, toolUitvoer, geheugenSamenvatting);
        var fallbackAntwoord = MaakFallbackAntwoord(bronTitel, bronInhoud, toolUitvoer);

        if (_provider == "ollama")
        {
            return await GenereerMetOllamaAsync(prompt, SysteemPrompt, fallbackAntwoord);
        }

        return await GenereerMetOpenAiAsync(prompt, SysteemPrompt, fallbackAntwoord);
    }

    public async Task<(string Antwoord, string AntwoordBron)> GenereerGespreksAntwoordAsync(
        string vraag,
        string geheugenSamenvatting)
    {
        var prompt = $"""
            Gebruikersbericht:
            {vraag}

            Sessie-geheugen:
            {geheugenSamenvatting}

            Reageer als support-agent. Begroet de gebruiker als dat past en vraag kort waarmee je kunt helpen.
            """;
        var fallbackAntwoord = "Hallo! Ik ben je support-agent. Ik kan helpen met bestellingen, terugbetalingen, wachtwoorden en facturen. Waarmee kan ik je helpen?";

        if (_provider == "ollama")
        {
            return await GenereerMetOllamaAsync(prompt, GespreksSysteemPrompt, fallbackAntwoord);
        }

        return await GenereerMetOpenAiAsync(prompt, GespreksSysteemPrompt, fallbackAntwoord);
    }

    public async Task<(string Antwoord, string AntwoordBron)> GenereerBestelnummerVraagAsync(
        string vraag,
        string geheugenSamenvatting)
    {
        var prompt = $"""
            Gebruikersbericht:
            {vraag}

            Sessie-geheugen:
            {geheugenSamenvatting}

            De gebruiker wil een bestelling laten opzoeken, maar heeft nog geen bestelnummer gegeven.
            Stel precies een korte verduidelijkingsvraag om het bestelnummer.
            Geef geen status, levertijd of verzendadvies zonder bestelnummer.
            """;
        var fallbackAntwoord = "Zou u uw bestelnummer kunnen doorgeven? Dan zoek ik het even voor u op.";

        if (_provider == "ollama")
        {
            return await GenereerMetOllamaAsync(prompt, GespreksSysteemPrompt, fallbackAntwoord);
        }

        return await GenereerMetOpenAiAsync(prompt, GespreksSysteemPrompt, fallbackAntwoord);
    }

    private async Task<(string Antwoord, string AntwoordBron)> GenereerMetOpenAiAsync(
        string prompt,
        string systeemPrompt,
        string fallbackAntwoord)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return (fallbackAntwoord, "fallback:no-api-key");
        }

        var payload = new
        {
            model = _model,
            instructions = systeemPrompt,
            input = prompt
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        string responseJson;
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request);
            responseJson = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "OpenAI API-call is mislukt");
            return (fallbackAntwoord, "fallback:api-error");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "OpenAI API-call duurde te lang");
            return (fallbackAntwoord, "fallback:api-error");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI gaf status {StatusCode}: {Body}", response.StatusCode, responseJson);
                return (fallbackAntwoord, "fallback:api-error");
            }
        }

        string? modelAntwoord;
        try
        {
            modelAntwoord = HaalTekstUitResponsesApi(responseJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OpenAI response bevatte geen bruikbare JSON");
            return (fallbackAntwoord, "fallback:no-output-text");
        }

        if (string.IsNullOrWhiteSpace(modelAntwoord))
        {
            return (fallbackAntwoord, "fallback:no-output-text");
        }

        return (modelAntwoord, $"openai:{_model}");
    }

    private async Task<(string Antwoord, string AntwoordBron)> GenereerMetOllamaAsync(
        string prompt,
        string systeemPrompt,
        string fallbackAntwoord)
    {
        var payload = new
        {
            model = _ollamaModel,
            stream = false,
            options = new { temperature = 0.2 },
            messages = new[]
            {
                new { role = "system", content = systeemPrompt },
                new { role = "user", content = prompt }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_ollamaBaseUrl}/api/chat");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        string responseJson;
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request);
            responseJson = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Ollama API-call is mislukt");
            return (fallbackAntwoord, "fallback:ollama-api-error");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Ollama API-call duurde te lang");
            return (fallbackAntwoord, "fallback:ollama-timeout");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama gaf status {StatusCode}: {Body}", response.StatusCode, responseJson);
                return (fallbackAntwoord, "fallback:ollama-api-error");
            }
        }

        string? modelAntwoord;
        try
        {
            modelAntwoord = HaalTekstUitOllamaApi(responseJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ollama response bevatte geen bruikbare JSON");
            return (fallbackAntwoord, "fallback:ollama-no-output-text");
        }

        if (string.IsNullOrWhiteSpace(modelAntwoord))
        {
            return (fallbackAntwoord, "fallback:ollama-no-output-text");
        }

        return (VerwijderDenkBlok(modelAntwoord), $"ollama:{_ollamaModel}");
    }

    private static string MaakPrompt(
        string vraag,
        string bronTitel,
        string bronInhoud,
        string toolUitvoer,
        string geheugenSamenvatting)
    {
        return $"""
            Klantvraag:
            {vraag}

            Kennisbankcontext:
            Titel: {bronTitel}
            Inhoud: {bronInhoud}

            Tooluitvoer:
            {(string.IsNullOrWhiteSpace(toolUitvoer) ? "Geen tool gebruikt." : toolUitvoer)}

            Sessie-geheugen:
            {geheugenSamenvatting}

            Schrijf een kort supportantwoord voor de klant. Gebruik alleen deze context.
            """;
    }

    private static string MaakFallbackAntwoord(string bronTitel, string bronInhoud, string toolUitvoer)
    {
        var toolZin = string.IsNullOrWhiteSpace(toolUitvoer)
            ? string.Empty
            : $" De besteltool geeft aan: {toolUitvoer}.";

        return $"Volgens '{bronTitel}' geldt: {bronInhoud}{toolZin}";
    }

    private static string? HaalTekstUitResponsesApi(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var deel in content.EnumerateArray())
            {
                if (deel.TryGetProperty("text", out var tekst) && tekst.ValueKind == JsonValueKind.String)
                {
                    return tekst.GetString();
                }
            }
        }

        return null;
    }

    private static string? HaalTekstUitOllamaApi(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String)
        {
            return content.GetString();
        }

        if (root.TryGetProperty("response", out var response) && response.ValueKind == JsonValueKind.String)
        {
            return response.GetString();
        }

        return null;
    }

    private static string VerwijderDenkBlok(string tekst)
    {
        return DenkBlokRegex().Replace(tekst, string.Empty).Trim();
    }

    [GeneratedRegex("<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex DenkBlokRegex();
}
