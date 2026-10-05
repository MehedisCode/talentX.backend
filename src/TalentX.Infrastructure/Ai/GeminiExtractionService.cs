using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Infrastructure.Ai.Helpers;
using TalentX.Infrastructure.Ai.Models;
using TalentX.Infrastructure.TextExtraction;

namespace TalentX.Infrastructure.Ai;

public sealed class GeminiExtractionService : IResumeExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly IAiConfigurationProvider _aiConfigProvider;
    private readonly ILogger<GeminiExtractionService> _logger;
    private readonly int _maxOutputTokens;

    private static readonly JsonSerializerOptions CaseInsensitive =
        new() { PropertyNameCaseInsensitive = true };

    public GeminiExtractionService(
        HttpClient httpClient,
        IAiConfigurationProvider aiConfigProvider,
        IConfiguration configuration,
        ILogger<GeminiExtractionService> logger)
    {
        _httpClient = httpClient;
        _aiConfigProvider = aiConfigProvider;
        _logger = logger;

        var baseUrl = Environment.GetEnvironmentVariable("GEMINI_BASE_URL")
            ?? configuration["GEMINI_BASE_URL"]
            ?? configuration["Gemini:BaseUrl"]
            ?? "https://generativelanguage.googleapis.com";
        _httpClient.BaseAddress = new Uri(baseUrl);

        var maxTokensValue = Environment.GetEnvironmentVariable("GEMINI_MAX_OUTPUT_TOKENS");
        _maxOutputTokens = int.TryParse(maxTokensValue, out var maxTokens) && maxTokens > 0
            ? maxTokens
            : 8192;
    }

    private const int MaxAttempts = 3;

    public async Task<ResumeExtractionResult> ExtractAsync(
        IUploadedFile file,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        string plainText;
        IReadOnlyList<string> documentLinks;

        await using (var stream = file.OpenReadStream())
        {
            (plainText, documentLinks) = extension switch
            {
                ".pdf" => PdfDocumentParser.ExtractContent(stream, cancellationToken),
                ".docx" => DocxDocumentParser.ExtractContent(stream, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported file type: {extension}")
            };
        }

        if (string.IsNullOrWhiteSpace(plainText))
        {
            throw new UnprocessableEntityException($"No extractable text found in the uploaded {extension.TrimStart('.').ToUpperInvariant()} file.");
        }

        var combinedLinks = TextUrlExtractor.CombineAndDeduplicate(plainText, documentLinks);
        var deterministicEmail = TextContactExtractor.ExtractEmail(plainText);
        var deterministicPhone = TextContactExtractor.ExtractPhone(plainText);

        var requestBody = GeminiPromptBuilder.BuildRequestBody(
            plainText,
            combinedLinks,
            deterministicEmail,
            deterministicPhone,
            _maxOutputTokens);

        var (modelName, apiKey) = await _aiConfigProvider.GetConfigAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"/v1beta/models/{modelName}:generateContent?key={apiKey}",
                    requestBody,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var statusCode = (int)response.StatusCode;
                    var isTransient = statusCode is 429 or 500 or 502 or 503 or 504;

                    _logger.LogWarning(
                        "Gemini API call attempt {Attempt} failed with status code {StatusCode}. Error: {ErrorContent}",
                        attempt, response.StatusCode, errorContent);

                    if (!isTransient || attempt == MaxAttempts)
                    {
                        throw new Exception($"Gemini API error: {response.StatusCode}");
                    }

                    await Task.Delay(attempt == 1 ? 1000 : 2000, cancellationToken);
                    continue;
                }

                var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(
                    CaseInsensitive, cancellationToken);

                var jsonText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    _logger.LogWarning("Gemini API returned an empty response on attempt {Attempt}.", attempt);
                    if (attempt == MaxAttempts)
                    {
                        throw new Exception("Gemini API returned an empty content part.");
                    }

                    await Task.Delay(attempt == 1 ? 1000 : 2000, cancellationToken);
                    continue;
                }

                var result = GeminiResponseMapper.MapToResult(
                    jsonText,
                    deterministicEmail,
                    deterministicPhone,
                    _logger,
                    plainText);

                if (IsEmptyOrIncomplete(result, plainText))
                {
                    if (attempt < MaxAttempts)
                    {
                        _logger.LogWarning(
                            "Gemini API returned empty/incomplete extraction data on attempt {Attempt}. Retrying...",
                            attempt);
                        await Task.Delay(attempt == 1 ? 1000 : 2000, cancellationToken);
                        continue;
                    }

                    _logger.LogError("Gemini API returned empty/incomplete extraction data after {MaxAttempts} attempts.", MaxAttempts);
                    throw new Exception("AI extraction returned incomplete or empty data.");
                }

                return result;
            }
            catch (Exception ex) when (ex is not InvalidOperationException && ex is not UnprocessableEntityException)
            {
                if (attempt == MaxAttempts || cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                _logger.LogWarning(ex, "Gemini API extraction failed on attempt {Attempt}. Retrying...", attempt);
                await Task.Delay(attempt == 1 ? 1000 : 2000, cancellationToken);
            }
        }

        throw new Exception("AI extraction failed after maximum retry attempts.");
    }

    private static bool IsEmptyOrIncomplete(ResumeExtractionResult result, string plainText)
    {
        if (plainText.Length < 100)
        {
            return false;
        }

        var hasName = !string.IsNullOrWhiteSpace(result.FullName);
        var hasExperience = result.WorkExperience.Count > 0;
        var hasEducation = result.Education.Count > 0;
        var hasSkills = result.TechnicalSkills.Count > 0 ||
                        result.SoftSkills.Count > 0 ||
                        result.ToolsAndPlatforms.Count > 0;

        return !hasName && !hasExperience && !hasEducation && !hasSkills;
    }
}
