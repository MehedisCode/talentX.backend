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

        var response = await _httpClient.PostAsJsonAsync(
            $"/v1beta/models/{modelName}:generateContent?key={apiKey}",
            requestBody,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Gemini API call failed with status code {StatusCode}. Error: {ErrorContent}",
                response.StatusCode, errorContent);
            throw new Exception($"Gemini API error: {response.StatusCode}");
        }

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(
            CaseInsensitive, cancellationToken);

        var jsonText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            _logger.LogError("Gemini API returned an empty response or invalid format.");
            throw new Exception("Gemini API returned an empty content part.");
        }

        return GeminiResponseMapper.MapToResult(
            jsonText,
            deterministicEmail,
            deterministicPhone,
            _logger,
            plainText);
    }
}
