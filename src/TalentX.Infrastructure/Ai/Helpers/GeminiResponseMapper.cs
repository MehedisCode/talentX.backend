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
        ILogger logger,
        string? resumeText = null)
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

            var workExperience = (result.WorkExperience ?? [])
                .Select(w =>
                {
                    var isCurrent = WorkExperienceNormalizer.IsCurrent(w.IsCurrent, w.EndDate);

                    return new WorkExperienceDto
                    {
                        CompanyName = w.CompanyName,
                        JobTitle = w.JobTitle,
                        StartDate = w.StartDate,
                        EndDate = WorkExperienceNormalizer.NormalizeEndDate(w.EndDate, isCurrent),
                        IsCurrent = isCurrent
                    };
                })
                .ToList();

            var education = (result.Education ?? [])
                .Select(e =>
                {
                    var (startDate, endDate) = EducationDateNormalizer.Normalize(e.StartDate, e.EndDate);

                    return new EducationDto
                    {
                        Institution = e.Institution,
                        Degree = e.Degree,
                        StartDate = startDate,
                        EndDate = endDate
                    };
                })
                .ToList();

            return new ResumeExtractionResult
            {
                FullName = result.FullName,
                Email = finalEmail,
                Phone = finalPhone,
                Location = result.Location,
                LinkedInUrl = result.LinkedInUrl,
                PortfolioUrls = PortfolioUrlClassifier.Filter(result.PortfolioUrls, resumeText),
                TechnicalSkills = result.TechnicalSkills ?? [],
                SoftSkills = result.SoftSkills ?? [],
                ToolsAndPlatforms = result.ToolsAndPlatforms ?? [],
                WorkExperience = ResumeChronology.SortNewestFirst(
                    workExperience,
                    w => w.IsCurrent ? ResumeChronology.CurrentKey : ResumeChronology.ToSortKey(w.StartDate)),
                Education = ResumeChronology.SortNewestFirst(
                    education,
                    e => ResumeChronology.ToSortKey(e.EndDate) ?? ResumeChronology.ToSortKey(e.StartDate))
            };
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse structured JSON from Gemini: {JsonText}", jsonText);
            throw;
        }
    }
}
