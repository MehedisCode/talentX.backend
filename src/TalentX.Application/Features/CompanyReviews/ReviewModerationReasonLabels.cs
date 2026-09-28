using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews;

public static class ReviewModerationReasonLabels
{
    public static string ToLabel(ReviewModerationReason reason) => reason switch
    {
        ReviewModerationReason.Spam => "Spam",
        ReviewModerationReason.InappropriateContent => "Inappropriate content",
        ReviewModerationReason.PersonalInformation => "Personal information",
        ReviewModerationReason.FalseOrMisleading => "False or misleading",
        ReviewModerationReason.OffTopic => "Off topic",
        _ => "Other"
    };
}
