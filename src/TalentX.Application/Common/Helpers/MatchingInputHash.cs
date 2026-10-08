using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TalentX.Application.Features.TalentMatching.DTOs;

namespace TalentX.Application.Common.Helpers;

/// <summary>
/// Content hash of the (job, candidate) inputs used for AI matching.
/// Identical inputs always produce the same hash, so a stored score whose
/// hash matches can be reused without calling the model again.
/// <see cref="ScoringVersion"/> is a salt: bump it whenever the prompt or
/// scoring logic changes to force a one-time full rescore.
/// </summary>
public static class MatchingInputHash
{
    public const string ScoringVersion = "v2";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public static string Compute(
        JobProfileForMatching job,
        TalentProfileForMatching candidate,
        string? scoringVersion = null)
    {
        var payload = new
        {
            v = scoringVersion ?? ScoringVersion,
            job = Canonicalize(job),
            candidate = Canonicalize(candidate)
        };

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static object Canonicalize(JobProfileForMatching job) => new
    {
        job.JobId,
        job.Title,
        job.CompanyName,
        job.Description,
        RequiredSkills = Sorted(job.RequiredSkills),
        job.ExperienceLevel,
        job.Location,
        job.WorkArrangement,
        job.City,
        job.Country
    };

    private static object Canonicalize(TalentProfileForMatching candidate) => new
    {
        candidate.TalentUserId,
        candidate.Name,
        candidate.Tagline,
        TechnicalSkills = Sorted(candidate.TechnicalSkills),
        SoftSkills = Sorted(candidate.SoftSkills),
        ToolsAndPlatforms = Sorted(candidate.ToolsAndPlatforms),
        candidate.TotalYearsExperience,
        candidate.ExpectedSalary,
        candidate.Location,
        SkillPropositions = Sorted(candidate.SkillPropositions),
        WorkExperiences = (candidate.WorkExperiences ?? new List<WorkExperienceSummary>())
            .Select(w => new { w.CompanyName, w.JobTitle, w.StartDate, w.EndDate, w.IsCurrent })
            .OrderBy(w => w.StartDate, StringComparer.Ordinal)
            .ThenBy(w => w.EndDate, StringComparer.Ordinal)
            .ThenBy(w => w.CompanyName, StringComparer.Ordinal)
            .ThenBy(w => w.JobTitle, StringComparer.Ordinal)
            .ThenBy(w => w.IsCurrent)
            .ToList(),
        Educations = (candidate.Educations ?? new List<EducationSummary>())
            .Select(e => new { e.Institution, e.Degree, e.StartDate, e.EndDate })
            .OrderBy(e => e.StartDate, StringComparer.Ordinal)
            .ThenBy(e => e.EndDate, StringComparer.Ordinal)
            .ThenBy(e => e.Institution, StringComparer.Ordinal)
            .ThenBy(e => e.Degree, StringComparer.Ordinal)
            .ToList()
    };

    private static List<string> Sorted(List<string>? values) =>
        (values ?? new List<string>())
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();
}
