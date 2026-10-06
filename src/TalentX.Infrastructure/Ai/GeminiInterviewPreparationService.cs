using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.Constants;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Infrastructure.Ai.Constants;
using TalentX.Infrastructure.Ai.Helpers;
using TalentX.Infrastructure.Ai.Models;

namespace TalentX.Infrastructure.Ai;

public sealed class GeminiInterviewPreparationService : IInterviewPreparationAiService
{
    private const int MaxAttempts = 3;
    private const int DefaultRetryBaseDelayMs = 500;

    private readonly HttpClient _httpClient;
    private readonly IAiConfigurationProvider _aiConfigProvider;
    private readonly ILogger<GeminiInterviewPreparationService> _logger;

    private readonly string _promptVersion;
    private readonly double _temperature;
    private readonly double _topP;
    private readonly int _maxOutputTokens;
    private readonly int _retryBaseDelayMs;

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
        IAiConfigurationProvider aiConfigProvider,
        IConfiguration configuration,
        ILogger<GeminiInterviewPreparationService> logger)
    {
        _httpClient = httpClient;
        _aiConfigProvider = aiConfigProvider;
        _logger = logger;

        var baseUrl = GetConfigValue(configuration, "GEMINI_BASE_URL", "Gemini:BaseUrl", GeminiConstants.DefaultBaseUrl);
        _httpClient.BaseAddress = new Uri(baseUrl);

        _promptVersion = GetConfigValue(configuration, "GEMINI_PROMPT_VERSION", "Gemini:PromptVersion", "v1");

        var tempStr = GetConfigValue(configuration, "GEMINI_TEMPERATURE", "Gemini:Temperature");
        _temperature = double.TryParse(tempStr, out var tempVal) ? tempVal : 0.7;

        var topPStr = GetConfigValue(configuration, "GEMINI_TOP_P", "Gemini:TopP");
        _topP = double.TryParse(topPStr, out var topPVal) ? topPVal : 0.95;

        var maxTokensStr = GetConfigValue(configuration, "GEMINI_MAX_OUTPUT_TOKENS", "Gemini:MaxOutputTokens");
        _maxOutputTokens = int.TryParse(maxTokensStr, out var maxTokensVal) ? maxTokensVal : 4096;

        var retryDelayStr = GetConfigValue(configuration, "GEMINI_RETRY_BASE_DELAY_MS", "Gemini:RetryBaseDelayMs");
        _retryBaseDelayMs = int.TryParse(retryDelayStr, out var retryVal) && retryVal >= 0
            ? retryVal
            : DefaultRetryBaseDelayMs;
    }

    public async Task<InterviewPreparationResult> GeneratePreparationAsync(
        InterviewPreparationContext context,
        CancellationToken cancellationToken = default)
    {
        var prompt = InterviewPreparationPromptBuilder.BuildPreparationPrompt(context);

        var call = await GenerateJsonTextAsync(
            prompt,
            _temperature,
            _maxOutputTokens,
            InterviewPreparationPromptBuilder.BuildPreparationSchema(),
            "generate-preparation",
            cancellationToken);

        return ParsePreparationResponse(call.Text) with { Metadata = BuildMetadata(call) };
    }

    public async Task<AdditionalQuestionsResult> GenerateAdditionalQuestionsAsync(
        AdditionalQuestionsContext context,
        CancellationToken cancellationToken = default)
    {
        var prompt = InterviewPreparationPromptBuilder.BuildAdditionalQuestionsPrompt(context);

        var call = await GenerateJsonTextAsync(
            prompt,
            _temperature,
            _maxOutputTokens,
            InterviewPreparationPromptBuilder.BuildAdditionalQuestionsSchema(),
            "generate-additional-questions",
            cancellationToken);

        return ParseAdditionalQuestionsResponse(call.Text) with { Metadata = BuildMetadata(call) };
    }

    public async Task<AnswersEvaluationResult> EvaluateAnswersAsync(
        AnswersEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var prompt = InterviewPreparationPromptBuilder.BuildEvaluationPrompt(context);

        var call = await GenerateJsonTextAsync(
            prompt,
            GeminiConstants.EvaluationTemperature,
            Math.Max(_maxOutputTokens, GeminiConstants.EvaluationMinOutputTokens),
            InterviewPreparationPromptBuilder.BuildEvaluationSchema(),
            "evaluate-answers",
            cancellationToken);

        return ParseEvaluationResponse(call.Text, context) with { Metadata = BuildMetadata(call) };
    }

    private AiCallMetadata BuildMetadata(GeminiCallResult call)
    {
        return new AiCallMetadata
        {
            ModelName = call.ModelName,
            PromptVersion = _promptVersion,
            LatencyMs = call.LatencyMs,
            InputTokenCount = call.Usage?.PromptTokenCount ?? 0,
            OutputTokenCount = call.Usage?.CandidatesTokenCount ?? 0
        };
    }

    private async Task<GeminiCallResult> GenerateJsonTextAsync(
        string prompt,
        double temperature,
        int maxOutputTokens,
        object? responseSchema,
        string operation,
        CancellationToken cancellationToken)
    {
        var (modelName, apiKey) = await _aiConfigProvider.GetConfigAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var generationConfig = new Dictionary<string, object>
        {
            ["temperature"] = temperature,
            ["top_p"] = _topP,
            ["max_output_tokens"] = maxOutputTokens,
            ["response_mime_type"] = "application/json"
        };
        if (responseSchema is not null)
        {
            generationConfig["response_schema"] = responseSchema;
        }

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
            generationConfig
        };

        var stopwatch = Stopwatch.StartNew();

        for (var attempt = 1; ; attempt++)
        {
            GeminiCallResult? success = null;
            Exception? failure = null;

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    $"/v1beta/models/{modelName}:generateContent?key={apiKey}",
                    requestBody,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var geminiResponse = await response.Content
                        .ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
                    var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        failure = new Exception("Gemini API returned empty content.");
                    }
                    else
                    {
                        stopwatch.Stop();
                        success = new GeminiCallResult(text, modelName, geminiResponse!.UsageMetadata, stopwatch.ElapsedMilliseconds);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Gemini API call {Operation} failed with status code {StatusCode} on attempt {Attempt}. Error: {ErrorContent}",
                        operation, response.StatusCode, attempt, errorContent);
                    failure = new Exception($"Gemini API error: {response.StatusCode}");

                    if (!IsTransient(response.StatusCode) || attempt >= MaxAttempts)
                    {
                        throw failure;
                    }
                }
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Gemini API call {Operation} hit a network error on attempt {Attempt}/{MaxAttempts}; retrying.",
                    operation, attempt, MaxAttempts);
                failure = ex;
            }

            if (success is not null)
            {
                _logger.LogInformation(
                    "Gemini call {Operation} succeeded: model {ModelName}, prompt {PromptVersion}, latency {LatencyMs}ms, tokens in {InputTokenCount} out {OutputTokenCount}, attempt {Attempt}",
                    operation, success.ModelName, _promptVersion, success.LatencyMs,
                    success.Usage?.PromptTokenCount ?? 0, success.Usage?.CandidatesTokenCount ?? 0, attempt);
                return success;
            }

            if (attempt >= MaxAttempts)
            {
                throw failure ?? new Exception("Gemini API call failed.");
            }

            var delayMs = _retryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
            _logger.LogWarning(
                "Gemini API call {Operation} attempt {Attempt}/{MaxAttempts} failed; retrying in {DelayMs}ms.",
                operation, attempt, MaxAttempts, delayMs);
            await Task.Delay(delayMs, cancellationToken);
        }
    }

    // 429 and 5xx are worth another attempt; other 4xx (bad key, malformed
    // request) will fail identically on retry, so they surface immediately.
    private static bool IsTransient(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode == HttpStatusCode.TooManyRequests || code >= 500;
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

    // Strict parse: a report the user waited for must not silently degrade.
    // Beyond JSON well-formedness this proves the report covers every answered
    // question exactly once — prompting alone cannot guarantee that.
    private AnswersEvaluationResult ParseEvaluationResponse(
        string rawText, AnswersEvaluationContext context)
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

        if (!parsed.TryGetProperty("questionFeedback", out var feedbackProp)
            || feedbackProp.ValueKind != JsonValueKind.Array)
        {
            _logger.LogError("Gemini evaluation response missing the questionFeedback array: {RawText}", rawText);
            throw new Exception("Gemini evaluation response is missing the questionFeedback array.");
        }

        var validatedFeedback = ValidateQuestionFeedback(feedbackProp, context, rawText);

        return new AnswersEvaluationResult
        {
            OverallScore = Math.Clamp(overallScore, 0, 100),
            Summary = summary,
            Strengths = GetPropertyAsJsonString(parsed, "strengths"),
            AreasToImprove = GetPropertyAsJsonString(parsed, "areasToImprove"),
            QuestionFeedback = JsonSerializer.Serialize(validatedFeedback)
        };
    }

    private List<ValidatedFeedbackEntry> ValidateQuestionFeedback(
        JsonElement feedbackProp,
        AnswersEvaluationContext context,
        string rawText)
    {
        var expected = context.AnsweredQuestions
            .Select(a => (a.Category, a.QuestionIndex))
            .ToList();

        var entries = new List<ValidatedFeedbackEntry>();
        var seen = new HashSet<(string Category, int QuestionIndex)>();

        foreach (var item in feedbackProp.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("category", out var catProp)
                || catProp.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(catProp.GetString())
                || !item.TryGetProperty("questionIndex", out var idxProp)
                || idxProp.ValueKind != JsonValueKind.Number
                || !idxProp.TryGetInt32(out var questionIndex)
                || questionIndex < 0
                || !item.TryGetProperty("score", out var scoreEntryProp)
                || scoreEntryProp.ValueKind != JsonValueKind.Number
                || !scoreEntryProp.TryGetInt32(out var entryScore)
                || !item.TryGetProperty("feedback", out var feedbackTextProp)
                || feedbackTextProp.ValueKind != JsonValueKind.String)
            {
                _logger.LogError(
                    "Gemini evaluation response contains a malformed questionFeedback entry: {RawText}",
                    rawText);
                throw new Exception("Gemini evaluation response contains a malformed questionFeedback entry.");
            }

            var category = catProp.GetString()!;
            var key = (category, questionIndex);

            if (!seen.Add(key))
            {
                _logger.LogError(
                    "Gemini evaluation response contains duplicate feedback for {Category}/{QuestionIndex}: {RawText}",
                    category, questionIndex, rawText);
                throw new Exception(
                    $"Gemini evaluation response contains duplicate feedback for {category}/{questionIndex}.");
            }

            string? suggestedAnswer = null;
            if (item.TryGetProperty("suggestedAnswer", out var suggestedProp)
                && suggestedProp.ValueKind == JsonValueKind.String)
            {
                suggestedAnswer = suggestedProp.GetString();
            }

            entries.Add(new ValidatedFeedbackEntry
            {
                Category = category,
                QuestionIndex = questionIndex,
                Score = Math.Clamp(entryScore, 0, 100),
                Feedback = feedbackTextProp.GetString() ?? string.Empty,
                SuggestedAnswer = suggestedAnswer
            });
        }

        var missing = expected.Where(e => !seen.Contains(e)).ToList();
        var unexpected = seen.Where(s => !expected.Contains(s)).ToList();

        if (missing.Count > 0 || unexpected.Count > 0)
        {
            _logger.LogError(
                "Gemini evaluation response failed coverage validation. Missing: {Missing}; unexpected: {Unexpected}; answered: {Answered}. Raw: {RawText}",
                string.Join(", ", missing.Select(m => $"{m.Category}/{m.QuestionIndex}")),
                string.Join(", ", unexpected.Select(u => $"{u.Category}/{u.QuestionIndex}")),
                string.Join(", ", expected.Select(e => $"{e.Category}/{e.QuestionIndex}")),
                rawText);
            throw new Exception(
                "Gemini evaluation response does not cover every answered question exactly once.");
        }

        return entries;
    }

    private sealed class ValidatedFeedbackEntry
    {
        public string Category { get; init; } = string.Empty;
        public int QuestionIndex { get; init; }
        public int Score { get; init; }
        public string Feedback { get; init; } = string.Empty;
        public string? SuggestedAnswer { get; init; }
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

    private sealed record GeminiCallResult(
        string Text, string ModelName, GeminiUsageMetadata? Usage, long LatencyMs);
}
