using System.Text.Json;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Models;
using TalentX.Infrastructure.Ai.Models;

namespace TalentX.Infrastructure.Ai.Helpers;

public static class GeminiResponseMapper
{
    private static readonly JsonSerializerOptions CaseInsensitive =
        new() { PropertyNameCaseInsensitive = true };

    public static ResumeExtractionResult MapToResult(
        string jsonText,
        string? deterministicEmail,
        string? deterministicPhone,
        ILogger logger)
    {
        try
        {
            var result = JsonSerializer.Deserialize<GeminiStructuredResult>(jsonText, CaseInsensitive);

            if (result is null)
                throw new Exception("Failed to deserialize structured AI result.");

            var finalEmail = !string.IsNullOrWhiteSpace(deterministicEmail)
                ? deterministicEmail
                : result.Email;

            var finalPhone = !string.IsNullOrWhiteSpace(deterministicPhone)
                ? deterministicPhone
                : result.Phone;

            return new ResumeExtractionResult
            {
                FullName = result.FullName,
                Email = finalEmail,
                Phone = finalPhone,
                Location = result.Location,
                LinkedInUrl = result.LinkedInUrl,
                PortfolioUrls = result.PortfolioUrls ?? [],
                TechnicalSkills = result.TechnicalSkills ?? [],
                SoftSkills = result.SoftSkills ?? [],
                ToolsAndPlatforms = result.ToolsAndPlatforms ?? [],
                WorkExperience = (result.WorkExperience ?? [])
                    .Select(w => new WorkExperienceDto
                    {
                        CompanyName = w.CompanyName,
                        JobTitle = w.JobTitle,
                        StartDate = w.StartDate,
                        EndDate = w.EndDate
                    }).ToList(),
                Education = (result.Education ?? [])
                    .Select(e => new EducationDto
                    {
                        Institution = e.Institution,
                        Degree = e.Degree,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate
                    }).ToList()
            };
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse structured JSON from Gemini: {JsonText}", jsonText);
            throw;
        }
    }
}
