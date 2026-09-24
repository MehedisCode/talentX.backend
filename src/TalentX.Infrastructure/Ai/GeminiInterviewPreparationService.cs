using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Infrastructure.Ai.Helpers;
using TalentX.Infrastructure.Ai.Models;

namespace TalentX.Infrastructure.Ai;

public sealed class GeminiInterviewPreparationService : IInterviewPreparationAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiInterviewPreparationService> _logger;

    private readonly string _apiKey;
    private readonly string _modelName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiInterviewPreparationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiInterviewPreparationService> logger)
    {
        _httpClient = httpClient;
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
    }

    public async Task<InterviewPreparationResult> GeneratePreparationAsync(
        InterviewPreparationContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var prompt = InterviewPreparationPromptBuilder.BuildPreparationPrompt(context);

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
                temperature = 0.7,
                response_mime_type = "application/json"
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/v1beta/models/{_modelName}:generateContent?key={_apiKey}",
            requestBody,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API call failed with status code {StatusCode}. Error: {ErrorContent}", response.StatusCode, errorContent);
            throw new Exception($"Gemini API error: {response.StatusCode}");
        }

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
        var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new Exception("Gemini API returned empty content.");
        }

        return ParsePreparationResponse(text);
    }

    public async Task<AdditionalQuestionsResult> GenerateAdditionalQuestionsAsync(
        AdditionalQuestionsContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var prompt = InterviewPreparationPromptBuilder.BuildAdditionalQuestionsPrompt(context);

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
                temperature = 0.7,
                response_mime_type = "application/json"
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/v1beta/models/{_modelName}:generateContent?key={_apiKey}",
            requestBody,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Gemini API call failed with status code {StatusCode}. Error: {ErrorContent}", response.StatusCode, errorContent);
            throw new Exception($"Gemini API error: {response.StatusCode}");
        }

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
        var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new Exception("Gemini API returned empty content.");
        }

        return ParseAdditionalQuestionsResponse(text);
    }

    private InterviewPreparationResult ParsePreparationResponse(string rawText)
    {
        var cleanJson = CleanJsonString(rawText);

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(cleanJson, JsonOptions);
            
            return new InterviewPreparationResult
            {
                TechnicalQuestions = GetPropertyAsJsonString(parsed, "technicalQuestions"),
                BehavioralQuestions = GetPropertyAsJsonString(parsed, "behavioralQuestions"),
                ExperienceBasedQuestions = GetPropertyAsJsonString(parsed, "experienceBasedQuestions"),
                RoleSpecificQuestions = GetPropertyAsJsonString(parsed, "roleSpecificQuestions"),
                KeyTopicsToPrepare = GetPropertyAsJsonString(parsed, "keyTopicsToPrepare"),
                SuggestedRevisionAreas = GetPropertyAsJsonString(parsed, "suggestedRevisionAreas")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini matching response JSON: {RawText}", rawText);
            throw new Exception("Failed to deserialize valid preparation result from AI.", ex);
        }
    }

    private AdditionalQuestionsResult ParseAdditionalQuestionsResponse(string rawText)
    {
        var cleanJson = CleanJsonString(rawText);

        try
        {
            var parsed = JsonSerializer.Deserialize<JsonElement>(cleanJson, JsonOptions);
            return new AdditionalQuestionsResult
            {
                AdditionalQuestions = GetPropertyAsJsonString(parsed, "additionalQuestions")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini matching response JSON: {RawText}", rawText);
            throw new Exception("Failed to deserialize valid additional questions result from AI.", ex);
        }
    }

    private static string GetPropertyAsJsonString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            return JsonSerializer.Serialize(prop, JsonOptions);
        }
        return "[]";
    }

    private static string CleanJsonString(string rawText)
    {
        var cleanJson = rawText.Trim();
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
        return cleanJson.Trim();
    }
}
