using TalentX.Application.Common.Exceptions;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Jobs.Common;

/// <summary>
/// Business rules for the Job feature, kept out of the entity on purpose — Job stays a
/// plain data holder; these rules belong to the use cases that orchestrate it.
/// </summary>
internal static class JobRules
{
    public static List<string> NormalizeSkills(List<string>? skills) =>
        skills?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? new List<string>();

    public static void EnsurePublishable(Job job)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(job.Title)) missing.Add(nameof(job.Title));
        if (string.IsNullOrWhiteSpace(job.CompanyName)) missing.Add(nameof(job.CompanyName));
        if (string.IsNullOrWhiteSpace(job.Description)) missing.Add(nameof(job.Description));
        if (string.IsNullOrWhiteSpace(job.Location)) missing.Add(nameof(job.Location));
        if (!Enum.IsDefined(job.EmploymentType)) missing.Add(nameof(job.EmploymentType));
        if (job.ExperienceLevel is null || !Enum.IsDefined(job.ExperienceLevel.Value)) missing.Add(nameof(job.ExperienceLevel));
        if (!Enum.IsDefined(job.WorkMode)) missing.Add(nameof(job.WorkMode));
        if (job.RequiredSkills.Count == 0) missing.Add(nameof(job.RequiredSkills));
        if (job.Vacancies <= 0) missing.Add(nameof(job.Vacancies));
        if (job.ExpireAt <= DateTime.UtcNow) missing.Add(nameof(job.ExpireAt));

        if (missing.Count > 0)
        {
            throw new ValidationException(
                $"Job cannot be published. Invalid or missing fields: {string.Join(", ", missing)}");
        }
    }
}
