namespace TalentX.Application.Features.JobApplications;

// simple normalization for JobApplication inputs
public static class JobApplicationRules
{
    public static string NormalizeRequired(string value) => value.Trim();

    public static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsManuallyAssignable(ApplicationStatus status) =>
        status is ApplicationStatus.Applied
            or ApplicationStatus.Reviewed
            or ApplicationStatus.Shortlisted
            or ApplicationStatus.Hired
            or ApplicationStatus.Rejected;

    public static bool IsRevertibleFrom(ApplicationStatus currentStatus) =>
        currentStatus is ApplicationStatus.Shortlisted
            or ApplicationStatus.Hired
            or ApplicationStatus.Rejected;
}
