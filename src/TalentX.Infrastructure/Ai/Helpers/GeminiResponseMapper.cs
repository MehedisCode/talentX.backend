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
                    var isCurrent = WorkExperienceNormalizer.IsCurrent(w.IsCurrent, w.EndDate, w.StartDate);

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

            var projects = (result.Projects ?? [])
                .Select(geminiProject =>
                {
                    var isOngoing = geminiProject.IsOngoing ||
                        (!string.IsNullOrWhiteSpace(geminiProject.EndDate) &&
                         WorkExperienceNormalizer.IsCurrent(geminiProject.IsOngoing, geminiProject.EndDate, geminiProject.StartDate));

                    return new ExtractedProjectDto
                    {
                        ProjectName = string.IsNullOrWhiteSpace(geminiProject.ProjectName) ? null : geminiProject.ProjectName.Trim(),
                        Role = string.IsNullOrWhiteSpace(geminiProject.Role) ? null : geminiProject.Role.Trim(),
                        ProjectType = NormalizeProjectType(geminiProject.ProjectType),
                        StartDate = string.IsNullOrWhiteSpace(geminiProject.StartDate) ? null : geminiProject.StartDate.Trim(),
                        EndDate = WorkExperienceNormalizer.NormalizeEndDate(geminiProject.EndDate, isOngoing),
                        IsOngoing = isOngoing,
                        Description = string.IsNullOrWhiteSpace(geminiProject.Description) ? null : geminiProject.Description.Trim(),
                        TechnologiesUsed = (geminiProject.TechnologiesUsed ?? [])
                            .Where(technology => !string.IsNullOrWhiteSpace(technology))
                            .Select(technology => technology.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList(),
                        ProjectUrl = string.IsNullOrWhiteSpace(geminiProject.ProjectUrl) ? null : geminiProject.ProjectUrl.Trim()
                    };
                })
                .ToList();

            var cleanTagline = string.IsNullOrWhiteSpace(result.Tagline)
                ? null
                : result.Tagline.Trim();

            var cleanShortBio = string.IsNullOrWhiteSpace(result.ShortBio)
                ? null
                : result.ShortBio.Trim();

            return new ResumeExtractionResult
            {
                FullName = result.FullName,
                Tagline = cleanTagline,
                ShortBio = cleanShortBio,
                Email = finalEmail,
                Phone = finalPhone,
                Location = result.Location,
                LinkedInUrl = result.LinkedInUrl,
                PortfolioUrls = PortfolioUrlClassifier.Filter(
                    result.PortfolioUrls,
                    resumeText,
                    new PortfolioIdentity(result.FullName, finalEmail, result.LinkedInUrl)),
                TechnicalSkills = result.TechnicalSkills ?? [],
                SoftSkills = result.SoftSkills ?? [],
                ToolsAndPlatforms = result.ToolsAndPlatforms ?? [],
                WorkExperience = ResumeChronology.SortNewestFirst(
                    workExperience,
                    experienceItem => experienceItem.IsCurrent ? ResumeChronology.CurrentKey : ResumeChronology.ToSortKey(experienceItem.StartDate)),
                Education = ResumeChronology.SortNewestFirst(
                    education,
                    educationItem => ResumeChronology.ToSortKey(educationItem.EndDate) ?? ResumeChronology.ToSortKey(educationItem.StartDate)),
                Projects = ResumeChronology.SortNewestFirst(
                    projects,
                    project => project.IsOngoing ? ResumeChronology.CurrentKey : (ResumeChronology.ToSortKey(project.EndDate) ?? ResumeChronology.ToSortKey(project.StartDate)))
            };
        }
        catch (JsonException jsonException)
        {
            logger.LogError(jsonException, "Failed to parse structured JSON from Gemini: {JsonText}", jsonText);
            throw;
        }
    }

    private static string? NormalizeProjectType(string? projectType)
    {
        if (string.IsNullOrWhiteSpace(projectType))
        {
            return null;
        }

        var trimmedType = projectType.Trim();
        if (string.Equals(trimmedType, "Professional", StringComparison.OrdinalIgnoreCase))
        {
            return "Professional";
        }

        if (string.Equals(trimmedType, "Personal", StringComparison.OrdinalIgnoreCase))
        {
            return "Personal";
        }

        if (string.Equals(trimmedType, "Academic", StringComparison.OrdinalIgnoreCase))
        {
            return "Academic";
        }

        return trimmedType;
    }
}
