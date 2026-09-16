using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Jobs.Moderation;

// One source of truth for how a deactivation reason is worded. The same label is used
// in the employer's email, the admin job details response and every history entry, so
// keeping it here stops those three from drifting apart.
public static class DeactivationReasonLabels
{
    public static string ToLabel(DeactivationReason reason) => reason switch
    {
        DeactivationReason.Spam => "Spam",
        DeactivationReason.InappropriateContent => "Inappropriate content",
        DeactivationReason.MisleadingInformation => "Misleading or invalid information",
        DeactivationReason.Discriminatory => "Discriminatory content",
        DeactivationReason.DuplicatePosting => "Duplicate posting",
        DeactivationReason.Outdated => "Outdated job",
        _ => "Other"
    };
}
