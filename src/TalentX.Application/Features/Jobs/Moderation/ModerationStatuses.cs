using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Moderation;

// The statuses admin moderation works with. Drafts and closed jobs are never moderated,
// so they stay out of the admin list, the status filter and the admin details endpoint.
public static class ModerationStatuses
{
    public static readonly IReadOnlyList<JobStatus> All =
    [
        JobStatus.Published,
        JobStatus.Deactivated,
        JobStatus.PendingReview
    ];

    public static bool Contains(JobStatus status) => All.Contains(status);
}
