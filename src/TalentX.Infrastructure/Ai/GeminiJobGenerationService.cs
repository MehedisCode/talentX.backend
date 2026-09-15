using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Ai.Models;

namespace TalentX.Infrastructure.Ai;

/// <summary>
/// Gemini-based implementation of job generation service.
/// Generates job descriptions and skills using the Gemini AI API.
/// </summary>
public sealed class GeminiJobGenerationService : IJobGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiJobGenerationService> _logger;
    private readonly string _apiKey;
    private readonly string _modelName;

    // JSON options for deserializing Gemini API responses
    private static readonly JsonSerializerOptions CaseInsensitive =
        new() { PropertyNameCaseInsensitive = true };

    public GeminiJobGenerationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiJobGenerationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Load Gemini configuration from environment or appsettings
        var baseUrl = Environment.GetEnvironmentVariable("GEMINI_BASE_URL")
            ?? configuration["GEMINI_BASE_URL"]
            ?? configuration["Gemini:BaseUrl"]
            ?? "https://generativelanguage.googleapis.com";
        _httpClient.BaseAddress = new Uri(baseUrl);

        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? configuration["GEMINI_API_KEY"]
            ?? configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");

        _modelName = Environment.GetEnvironmentVariable("GEMINI_MODEL_NAME")
            ?? configuration["GEMINI_MODEL_NAME"]
            ?? configuration["Gemini:ModelName"]
            ?? "gemini-3.7-flash";
    }

    /// <inheritdoc />
    public async Task<JobGenerationResult> GenerateDescriptionAsync(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement,
        List<string>? requiredSkills = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildDescriptionPrompt(title, companyName, employmentType,
            experienceLevel, workArrangement, requiredSkills);

        var response = await CallGeminiAsync(prompt, cancellationToken);

        return new JobGenerationResult { Description = response };
    }

    /// <inheritdoc />
    public async Task<JobSkillsResult> GenerateSkillsAsync(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildSkillsPrompt(title, companyName, employmentType,
            experienceLevel, workArrangement);

        var response = await CallGeminiAsync(prompt, cancellationToken);

        // Parse the comma-separated skills from Gemini response
        var skills = response.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList();

        return new JobSkillsResult { Skills = skills };
    }

    /// <inheritdoc />
    public async Task<JobRequirementsResult> GenerateRequirementsAsync(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement,
        List<string>? requiredSkills = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildRequirementsPrompt(title, companyName, employmentType,
            experienceLevel, workArrangement, requiredSkills);

        var response = await CallGeminiAsync(prompt, cancellationToken);

        return new JobRequirementsResult { Requirements = response };
    }

    /// <summary>
    /// Calls the Gemini API with the given prompt and returns the text response.
    /// </summary>
    private async Task<string> CallGeminiAsync(string prompt, CancellationToken cancellationToken)
    {
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
                temperature = 0.7,  // Slightly creative for job content
                top_p = 0.95,
                max_output_tokens = 2048
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
                "Gemini API call failed with status code {StatusCode}. Error: {ErrorContent}",
                response.StatusCode, errorContent);
            throw new Exception($"Gemini API error: {response.StatusCode}");
        }

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(
            CaseInsensitive, cancellationToken);

        var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogError("Gemini API returned an empty response.");
            throw new Exception("Gemini API returned empty content.");
        }

        return text;
    }

    /// <summary>
    /// Builds the prompt for generating a job description.
    /// </summary>
    private static string BuildDescriptionPrompt(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement,
        List<string>? requiredSkills)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert HR copywriter specializing in tech job postings.");
        sb.AppendLine("Generate a professional, compelling job description based on the details below.");
        sb.AppendLine("The description should be 200-400 words, well-structured with clear sections.");
        sb.AppendLine("Use a professional but engaging tone that attracts quality candidates.");
        sb.AppendLine();
        sb.AppendLine("JOB DETAILS:");
        sb.AppendLine($"- Job Title: {title}");
        sb.AppendLine($"- Company: {companyName}");
        sb.AppendLine($"- Employment Type: {employmentType}");
        sb.AppendLine($"- Experience Level: {experienceLevel}");
        sb.AppendLine($"- Work Arrangement: {workArrangement}");

        if (requiredSkills?.Any() == true)
        {
            sb.AppendLine($"- Required Skills: {string.Join(", ", requiredSkills)}");
        }

        sb.AppendLine();
        sb.AppendLine("OUTPUT FORMAT:");
        sb.AppendLine("Return ONLY the job description text. Do not include any headers, labels, or JSON.");
        sb.AppendLine("Structure it with natural paragraphs covering:");
        sb.AppendLine("1. Opening paragraph (company + role overview)");
        sb.AppendLine("2. Key responsibilities");
        sb.AppendLine("3. Required qualifications");
        sb.AppendLine("4. What we offer");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the prompt for generating relevant skills for a job.
    /// </summary>
    private static string BuildSkillsPrompt(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a tech hiring expert. Generate a list of relevant skills for this job.");
        sb.AppendLine("Return ONLY a comma-separated list of skills. No explanations or labels.");
        sb.AppendLine();
        sb.AppendLine("JOB DETAILS:");
        sb.AppendLine($"- Job Title: {title}");
        sb.AppendLine($"- Company: {companyName}");
        sb.AppendLine($"- Employment Type: {employmentType}");
        sb.AppendLine($"- Experience Level: {experienceLevel}");
        sb.AppendLine($"- Work Arrangement: {workArrangement}");
        sb.AppendLine();
        sb.AppendLine("REQUIREMENTS:");
        sb.AppendLine("- Return 8-15 relevant technical and soft skills");
        sb.AppendLine("- Skills should match the job title and experience level");
        sb.AppendLine("- Include both technical skills (e.g., C#, React) and soft skills (e.g., Communication)");
        sb.AppendLine("- Use industry-standard skill names");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the prompt for generating a job's requirements section
    /// (experience range, education, language proficiency) as bullet points.
    /// </summary>
    private static string BuildRequirementsPrompt(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement,
        List<string>? requiredSkills)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a tech hiring expert. Generate a requirements section for this job posting.");
        sb.AppendLine("Return ONLY a bullet-point list — each bullet on its own line, starting with \"- \".");
        sb.AppendLine("Do not include any headers, labels, explanations, or JSON — just the bullet lines.");
        sb.AppendLine();
        sb.AppendLine("JOB DETAILS:");
        sb.AppendLine($"- Job Title: {title}");
        sb.AppendLine($"- Company: {companyName}");
        sb.AppendLine($"- Employment Type: {employmentType}");
        sb.AppendLine($"- Experience Level: {experienceLevel}");
        sb.AppendLine($"- Work Arrangement: {workArrangement}");

        if (requiredSkills?.Any() == true)
        {
            sb.AppendLine($"- Required Skills: {string.Join(", ", requiredSkills)}");
        }

        sb.AppendLine();
        sb.AppendLine("The bullet list must cover exactly these three points, in this order:");
        sb.AppendLine("1. Years of professional experience required, expressed as a range appropriate");
        sb.AppendLine("   to the given Experience Level (e.g. \"3-5 years of relevant experience\").");
        sb.AppendLine("2. The education level or field required (e.g. \"Bachelor's degree in Computer");
        sb.AppendLine("   Science or a related field, or equivalent practical experience\").");
        sb.AppendLine("3. A language proficiency requirement appropriate to the role (e.g. \"Fluent");
        sb.AppendLine("   written and spoken English\").");
        sb.AppendLine("Keep each bullet to a single concise sentence.");

        return sb.ToString();
    }
}
