namespace TalentX.Application.Features.JobApplications;

// simple normalization for JobApplication inputs
public static class JobApplicationRules
{
    public static string NormalizeRequired(string value) => value.Trim();

    public static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
