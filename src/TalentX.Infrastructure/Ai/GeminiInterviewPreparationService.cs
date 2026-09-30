using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Infrastructure.Ai.Constants;
using TalentX.Infrastructure.Ai.Helpers;
using TalentX.Infrastructure.Ai.Models;

namespace TalentX.Infrastructure.Ai;

public sealed class GeminiInterviewPreparationService : IInterviewPreparationAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiInterviewPreparationService> _logger;

    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly double _temperature;
    private readonly double _topP;
    private readonly int _maxOutputTokens;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static string GetConfigValue(IConfiguration config, string envKey, string jsonKey, string defaultValue = "")
    {
        var val = config[envKey];
        if (!string.IsNullOrWhiteSpace(val)) return val;
        
        val = config[jsonKey];
        if (!string.IsNullOrWhiteSpace(val)) return val;
        
        return defaultValue;
    }

    public GeminiInterviewPreparationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiInterviewPreparationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = GetConfigValue(configuration, "GEMINI_BASE_URL", "Gemini:BaseUrl", GeminiConstants.DefaultBaseUrl);
        _httpClient.BaseAddress = new Uri(baseUrl);

        _apiKey = GetConfigValue(configuration, "GEMINI_API_KEY", "Gemini:ApiKey");

        _modelName = GetConfigValue(configuration, "GEMINI_MODEL_NAME", "Gemini:ModelName", GeminiConstants.DefaultModelName);

        var tempStr = GetConfigValue(configuration, "GEMINI_TEMPERATURE", "Gemini:Temperature");
        _temperature = double.TryParse(tempStr, out var tempVal) ? tempVal : 0.7;

        var topPStr = GetConfigValue(configuration, "GEMINI_TOP_P", "Gemini:TopP");
        _topP = double.TryParse(topPStr, out var topPVal) ? topPVal : 0.95;

        var maxTokensStr = GetConfigValue(configuration, "GEMINI_MAX_OUTPUT_TOKENS", "Gemini:MaxOutputTokens");
        _maxOutputTokens = int.TryParse(maxTokensStr, out var maxTokensVal) ? maxTokensVal : 4096;
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
                temperature = _temperature,
                top_p = _topP,
                max_output_tokens = _maxOutputTokens,
                response_mime_type = "application/json",
                response_schema = InterviewPreparationPromptBuilder.BuildPreparationSchema()
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
                temperature = _temperature,
                top_p = _topP,
                max_output_tokens = _maxOutputTokens,
                response_mime_type = "application/json",
                response_schema = InterviewPreparationPromptBuilder.BuildAdditionalQuestionsSchema()
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

    public async Task<AnswersEvaluationResult> EvaluateAnswersAsync(
        AnswersEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var prompt = InterviewPreparationPromptBuilder.BuildEvaluationPrompt(context);

        // Evaluation is judgment-heavy: low temperature for consistent scoring, and a
        // higher output budget because per-question feedback scales with answered count.
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
                temperature = 0.3,
                top_p = _topP,
                max_output_tokens = Math.Max(_maxOutputTokens, 8192),
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

        return ParseEvaluationResponse(text);
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

    // Strict parse: a report the user waited for must not silently degrade, so a
    // missing/garbage overallScore fails the request (mapped to 502 by the handler).
    private AnswersEvaluationResult ParseEvaluationResponse(string rawText)
    {
        var cleanJson = CleanJsonString(rawText);

        JsonElement parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<JsonElement>(cleanJson, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini evaluation response JSON: {RawText}", rawText);
            throw new Exception("Failed to deserialize valid evaluation result from AI.", ex);
        }

        if (!parsed.TryGetProperty("overallScore", out var scoreProp)
            || scoreProp.ValueKind != JsonValueKind.Number
            || !scoreProp.TryGetInt32(out var overallScore))
        {
            _logger.LogError("Gemini evaluation response missing a numeric overallScore: {RawText}", rawText);
            throw new Exception("Gemini evaluation response is missing a numeric overallScore.");
        }

        var summary = string.Empty;
        if (parsed.TryGetProperty("summary", out var summaryProp)
            && summaryProp.ValueKind == JsonValueKind.String)
        {
            summary = summaryProp.GetString() ?? string.Empty;
        }

        return new AnswersEvaluationResult
        {
            OverallScore = overallScore,
            Summary = summary,
            Strengths = GetPropertyAsJsonString(parsed, "strengths"),
            AreasToImprove = GetPropertyAsJsonString(parsed, "areasToImprove"),
            QuestionFeedback = GetPropertyAsJsonString(parsed, "questionFeedback")
        };
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
