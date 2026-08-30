using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs;

// Normalization and business rules for Job that used to live on the entity.
// Centralized here (rather than duplicated per-handler) so CreateJob and
// UpdateJob can't drift out of sync on what "valid" or "publishable" means.
public static class JobRules
{
    public static string NormalizeText(string value) => value.Trim();

    public static List<string> NormalizeSkills(List<string>? skills) =>
        skills?.Where(s => !string.IsNullOrWhiteSpace(s))
               .Select(s => s.Trim())
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToList() ?? new List<string>();

    public static void EnsurePublishable(Job job)
    {
        if (job.Status != JobStatus.Draft)
            throw new InvalidOperationException("Only draft jobs can be published.");

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(job.Title))
            missing.Add(nameof(job.Title));

        if (string.IsNullOrWhiteSpace(job.CompanyName))
            missing.Add(nameof(job.CompanyName));

        if (string.IsNullOrWhiteSpace(job.Description))
            missing.Add(nameof(job.Description));

        if (string.IsNullOrWhiteSpace(job.Location))
            missing.Add(nameof(job.Location));

        if (!Enum.IsDefined(job.EmploymentType))
            missing.Add(nameof(job.EmploymentType));

        if (!Enum.IsDefined(job.ExperienceLevel))
            missing.Add(nameof(job.ExperienceLevel));

        if (!Enum.IsDefined(job.WorkArrangement))
            missing.Add(nameof(job.WorkArrangement));

        if (job.RequiredSkills.Count == 0)
            missing.Add(nameof(job.RequiredSkills));

        if (job.Vacancies <= 0)
            missing.Add(nameof(job.Vacancies));

        if (job.ExpireAt <= DateTime.UtcNow)
            missing.Add(nameof(job.ExpireAt));

        // Conditional validation for wizard fields based on WorkArrangement
        if (IsLocationRequired(job.WorkArrangement))
        {
            if (string.IsNullOrWhiteSpace(job.City))
                missing.Add(nameof(job.City));

            if (string.IsNullOrWhiteSpace(job.Country))
                missing.Add(nameof(job.Country));
        }

        if (IsRemoteTimezoneRequired(job.WorkArrangement))
        {
            if (!job.MinOverlapHours.HasValue || job.MinOverlapHours.Value <= 0)
                missing.Add(nameof(job.MinOverlapHours));
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Job cannot be published. Invalid or missing fields: {string.Join(", ", missing)}"
            );
        }
    }

    private static bool IsLocationRequired(WorkArrangement a) =>
        a is WorkArrangement.OnSite or WorkArrangement.Hybrid;

    private static bool IsRemoteTimezoneRequired(WorkArrangement a) =>
        a is WorkArrangement.AnywhereFullRemote;
}
