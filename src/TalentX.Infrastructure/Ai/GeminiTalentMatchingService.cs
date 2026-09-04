using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Infrastructure.Ai.Models;

namespace TalentX.Infrastructure.Ai;

public sealed class GeminiTalentMatchingService : ITalentMatchingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiTalentMatchingService> _logger;
    private readonly string _apiKey;
    private readonly string _modelName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiTalentMatchingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiTalentMatchingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = Environment.GetEnvironmentVariable("GEMINI_BASE_URL")
            ?? configuration["GEMINI_BASE_URL"]
            ?? configuration["Gemini:BaseUrl"]
            ?? "https://generativelanguage.googleapis.com";
        _httpClient.BaseAddress = new Uri(baseUrl);

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? configuration["GEMINI_API_KEY"]
            ?? configuration["Gemini:ApiKey"]
            ?? string.Empty;

        _modelName = Environment.GetEnvironmentVariable("GEMINI_MODEL_NAME")
            ?? configuration["GEMINI_MODEL_NAME"]
            ?? configuration["Gemini:ModelName"]
            ?? "gemini-3.7-flash";
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

        var prompt = BuildPrompt(request);

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
                temperature = 0.1,
                top_p = 0.95,
                max_output_tokens = 4096
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

    private static string BuildPrompt(TalentMatchingRequest request)
    {
        var jobJson = JsonSerializer.Serialize(request.Job, new JsonSerializerOptions { WriteIndented = true });
        var candidatesJson = JsonSerializer.Serialize(request.Candidates, new JsonSerializerOptions { WriteIndented = true });

        return $$"""
You are an expert AI recruiting and candidate matching engine.
Your task is to analyze candidate profiles against a specific job posting and calculate a numeric compatibility match score (0 to 100) along with a concise explanation for each candidate.

CRITICAL MATCHING CRITERIA TO EVALUATE:
1. Skills & Technologies: Compare job's RequiredSkills with candidate's TechnicalSkills, ToolsAndPlatforms, and SkillPropositions.
2. Experience Level & Years: Compare job's ExperienceLevel with candidate's TotalYearsExperience.
3. Job Title & Past Roles: Compare job title and description with candidate's past WorkExperience job titles and companies.
4. Industry Experience: Consider relevance of past companies and roles where available.
5. Location & Work Arrangement: Compare job's Location, City, Country, and WorkArrangement (Remote, Hybrid, OnSite) with candidate's Location. Remote jobs are location-flexible.
6. Education Level: Consider candidate's degrees and fields of study from Educations where available.
7. Salary Expectation: Consider candidate's ExpectedSalary alignment where available.

JOB DETAILS:
{{jobJson}}

CANDIDATES:
{{candidatesJson}}

RESPONSE INSTRUCTIONS:
Return ONLY valid JSON with no preamble or markdown formatting, following this exact schema:
{
  "matches": [
    {
      "talentUserId": "the candidate's talentUserId",
      "score": 85.5,
      "explanation": "Clear, concise 1-2 sentence justification explaining why this candidate matches the role based on skills, experience, and criteria."
    }
  ]
}
""";
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
