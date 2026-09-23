using TalentX.Application.Features.JobAlerts.Constants;

namespace TalentX.Application.Features.Jobs.Moderation;

public static class ModerationLinks
{
    private static string BaseUrl
    {
        get
        {
            var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
            if (!string.IsNullOrWhiteSpace(corsOrigins))
            {
                var firstOrigin = corsOrigins
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(firstOrigin))
                {
                    return firstOrigin.TrimEnd('/');
                }
            }

            return JobAlertConstants.DefaultFrontendBaseUrl;
        }
    }

    // Where the employer fixes a deactivated job.
    public static string EditJob(Guid jobId) => $"{BaseUrl}/job/{jobId}/edit";

    // The public job page, used once the job is live again.
    public static string ViewJob(Guid jobId) => $"{BaseUrl}/jobs/{jobId}";
}
