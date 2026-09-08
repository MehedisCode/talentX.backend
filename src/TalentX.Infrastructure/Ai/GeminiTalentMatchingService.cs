using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Infrastructure.Ai.Models;
using TalentX.Infrastructure.Ai.Prompts;

namespace TalentX.Infrastructure.Ai;

public sealed class GeminiTalentMatchingService : ITalentMatchingService
{
    private readonly HttpClient _httpClient;
    private readonly ITalentMatchingPromptFactory _promptFactory;
    private readonly ILogger<GeminiTalentMatchingService> _logger;

    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly string _promptVersion;
    private readonly double _temperature;
    private readonly double _topP;
    private readonly int _maxOutputTokens;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiTalentMatchingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ITalentMatchingPromptFactory promptFactory,
        ILogger<GeminiTalentMatchingService> logger)
    {
        _httpClient = httpClient;
        _promptFactory = promptFactory;
        _logger = logger;

        var baseUrl = configuration["GEMINI_BASE_URL"]
            ?? configuration["Gemini:BaseUrl"]
            ?? "https://generativelanguage.googleapis.com";
        _httpClient.BaseAddress = new Uri(baseUrl);

        _apiKey = configuration["GEMINI_API_KEY"]
            ?? configuration["Gemini:ApiKey"]
            ?? string.Empty;

        _modelName = configuration["GEMINI_MODEL_NAME"]
            ?? configuration["Gemini:ModelName"]
            ?? "gemini-3.7-flash";

        _promptVersion = configuration["GEMINI_PROMPT_VERSION"]
            ?? configuration["Gemini:PromptVersion"]
            ?? "v1";

        var tempStr = configuration["GEMINI_TEMPERATURE"] ?? configuration["Gemini:Temperature"];
        _temperature = double.TryParse(tempStr, out var tempVal) ? tempVal : 0.1;

        var topPStr = configuration["GEMINI_TOP_P"] ?? configuration["Gemini:TopP"];
        _topP = double.TryParse(topPStr, out var topPVal) ? topPVal : 0.95;

        var maxTokensStr = configuration["GEMINI_MAX_OUTPUT_TOKENS"] ?? configuration["Gemini:MaxOutputTokens"];
        _maxOutputTokens = int.TryParse(maxTokensStr, out var maxTokensVal) ? maxTokensVal : 4096;
    }

    public async Task<TalentMatchingResult> MatchAsync(
        TalentMatchingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Candidates.Count == 0)
        {
            return new TalentMatchingResult { Matches = new List<CandidateMatchResult>() };
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Gemini API key is not configured.");
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var promptEngine = _promptFactory.GetPrompt(_promptVersion);
        var prompt = promptEngine.BuildPrompt(request);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = _temperature,
                top_p = _topP,
                max_output_tokens = _maxOutputTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/v1beta/models/{_modelName}:generateContent?key={_apiKey}",
            requestBody,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gemini matching API call failed with status code {StatusCode}. Error: {ErrorContent}",
                response.StatusCode, errorContent);
            throw new Exception($"Gemini matching API error: {response.StatusCode}");
        }

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(
            JsonOptions, cancellationToken);

        var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogError("Gemini API returned empty matching response.");
            throw new Exception("Gemini API returned empty content.");
        }

        return ParseGeminiResponse(text);
    }

    private TalentMatchingResult ParseGeminiResponse(string rawText)
    {
        var cleanJson = rawText.Trim();

        // Strip markdown code block if present
        if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleanJson = cleanJson[7..];
        }
        else if (cleanJson.StartsWith("```"))
        {
            cleanJson = cleanJson[3..];
        }

        if (cleanJson.EndsWith("```"))
        {
            cleanJson = cleanJson[..^3];
        }

        cleanJson = cleanJson.Trim();

        try
        {
            var result = JsonSerializer.Deserialize<TalentMatchingResult>(cleanJson, JsonOptions);
            if (result?.Matches != null)
            {
                // Ensure scores are bounded between 0 and 100
                foreach (var match in result.Matches)
                {
                    match.Score = Math.Clamp(Math.Round(match.Score, 2), 0m, 100m);
                }
                return result;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini matching response JSON: {RawText}", rawText);
        }

        throw new Exception("Failed to deserialize valid talent matching result from AI.");
    }
}
