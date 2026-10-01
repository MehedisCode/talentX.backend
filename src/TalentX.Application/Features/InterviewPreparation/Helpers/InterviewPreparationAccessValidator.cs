using TalentX.Application.Common.Exceptions;
using TalentX.Domain.Entities;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Features.InterviewPreparation.Helpers;

public static class InterviewPreparationAccessValidator
{
    public static void EnsureUserCanAccess(InterviewPreparationEntity preparation, string userId)
    {
        if (preparation.TalentUserId != userId)
            throw new ForbiddenException("You do not have access to this interview preparation.");
    }

    public static void EnsureUserCanAccess(InterviewInvitation invitation, string userId)
    {
        if (invitation.TalentUserId != userId)
            throw new ForbiddenException("You do not have access to this interview.");
    }

    public static void EnsurePreparationIsAccessible(InterviewPreparationEntity preparation, TimeSpan accessWindow)
    {
        if (!preparation.IsAccessible(accessWindow))
            throw new ForbiddenException("Preparation is no longer accessible as the interview has already taken place.");
    }
}
