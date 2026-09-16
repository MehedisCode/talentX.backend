using TalentX.Application.Features.JobAlerts.Constants;

namespace TalentX.Application.Features.Jobs.Moderation;

public static class ModerationLinks
{
    private static string BaseUrl =>
        Environment.GetEnvironmentVariable("FRONTEND_URL")?.TrimEnd('/')
        ?? JobAlertConstants.DefaultFrontendBaseUrl;

    // Where the employer fixes a deactivated job.
    public static string EditJob(Guid jobId) => $"{BaseUrl}/job/{jobId}/edit";

    // The public job page, used once the job is live again.
    public static string ViewJob(Guid jobId) => $"{BaseUrl}/jobs/{jobId}";
}
