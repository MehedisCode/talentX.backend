using FluentAssertions;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Interviews;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Interviews;

public class InterviewRulesTests
{
    private static InterviewInvitation CreateOnlineAssessmentInvitation(DateTime? assessmentDeadline)
    {
        return new InterviewInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employer-1",
            "talent-1",
            InterviewType.OnlineAssessment,
            assessmentUrl: "https://assessment.example.com",
            assessmentDeadline: assessmentDeadline);
    }

    private static InterviewInvitation CreateOnCallInvitation(params DateTime[] timeslotStarts)
    {
        var invitation = new InterviewInvitation(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "employer-1",
            "talent-1",
            InterviewType.OnCall,
            locationOrLink: "https://meet.example.com/room");

        foreach (var start in timeslotStarts)
        {
            invitation.AddTimeslot(start, start.AddMinutes(30));
        }

        return invitation;
    }

    [Fact]
    public void EnsureNotExpired_OnlineAssessment_WhenDeadlineInFuture_DoesNotThrow()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(1));

        var act = () => InterviewRules.EnsureNotExpired(invitation, DateTime.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNotExpired_OnlineAssessment_WhenDeadlinePassed_Throws()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(-1));

        var act = () => InterviewRules.EnsureNotExpired(invitation, DateTime.UtcNow);

        act.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void EnsureNotExpired_OnlineAssessment_WhenNoDeadlineSet_DoesNotThrow()
    {
        var invitation = CreateOnlineAssessmentInvitation(null);

        var act = () => InterviewRules.EnsureNotExpired(invitation, DateTime.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNotExpired_OnCall_WhenAtLeastOneTimeslotInFuture_DoesNotThrow()
    {
        var now = DateTime.UtcNow;
        var invitation = CreateOnCallInvitation(now.AddHours(-2), now.AddHours(3));

        var act = () => InterviewRules.EnsureNotExpired(invitation, now);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNotExpired_OnCall_WhenEveryTimeslotHasPassed_Throws()
    {
        var now = DateTime.UtcNow;
        var invitation = CreateOnCallInvitation(now.AddHours(-3), now.AddHours(-1));

        var act = () => InterviewRules.EnsureNotExpired(invitation, now);

        act.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void EnsureNotExpired_OnCall_WhenNoTimeslotsOffered_DoesNotThrow()
    {
        var invitation = CreateOnCallInvitation();

        var act = () => InterviewRules.EnsureNotExpired(invitation, DateTime.UtcNow);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureIsPending_WhenPending_DoesNotThrow()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(1));

        var act = () => InterviewRules.EnsureIsPending(invitation);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(InterviewInvitationStatus.Accepted)]
    [InlineData(InterviewInvitationStatus.Declined)]
    [InlineData(InterviewInvitationStatus.Cancelled)]
    public void EnsureIsPending_WhenNotPending_ThrowsConflict(InterviewInvitationStatus status)
    {
        var invitation = CreateOnCallInvitation(DateTime.UtcNow.AddHours(1));
        switch (status)
        {
            case InterviewInvitationStatus.Accepted:
                invitation.Accept(invitation.Timeslots[0].Id, DateTime.UtcNow.AddHours(1));
                break;
            case InterviewInvitationStatus.Declined:
                invitation.Decline();
                break;
            case InterviewInvitationStatus.Cancelled:
                invitation.Cancel();
                break;
        }

        var act = () => InterviewRules.EnsureIsPending(invitation);

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void IsPast_UsesScheduledAt_WhenInvitationHasBeenAccepted()
    {
        var now = DateTime.UtcNow;
        var invitation = CreateOnCallInvitation(now.AddDays(-5));
        invitation.Accept(invitation.Timeslots[0].Id, now.AddHours(-1));

        InterviewRules.IsPast(invitation, now).Should().BeTrue();
    }

    [Fact]
    public void IsPast_OnlineAssessment_FalseBeforeDeadline()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddHours(1));

        InterviewRules.IsPast(invitation, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsPast_OnCall_FalseWhileAnyTimeslotStillUpcoming()
    {
        var now = DateTime.UtcNow;
        var invitation = CreateOnCallInvitation(now.AddHours(-2), now.AddHours(3));

        InterviewRules.IsPast(invitation, now).Should().BeFalse();
    }

    [Fact]
    public void IsPast_NoDatesAtAll_IsFalse()
    {
        var invitation = CreateOnCallInvitation();

        InterviewRules.IsPast(invitation, DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void GetEffectiveAt_PrefersScheduledAt_ThenDeadline_ThenEarliestTimeslot_ThenSentAt()
    {
        var now = DateTime.UtcNow;

        var scheduled = CreateOnCallInvitation(now.AddDays(1));
        scheduled.Accept(scheduled.Timeslots[0].Id, now.AddDays(2));
        InterviewRules.GetEffectiveAt(scheduled).Should().Be(now.AddDays(2));

        var deadlineOnly = CreateOnlineAssessmentInvitation(now.AddDays(3));
        InterviewRules.GetEffectiveAt(deadlineOnly).Should().Be(now.AddDays(3));

        var timeslotsOnly = CreateOnCallInvitation(now.AddDays(5), now.AddDays(4));
        InterviewRules.GetEffectiveAt(timeslotsOnly).Should().Be(now.AddDays(4));

        var noDatesAtAll = CreateOnCallInvitation();
        InterviewRules.GetEffectiveAt(noDatesAtAll).Should().Be(noDatesAtAll.SentAt);
    }

    [Fact]
    public void GetTalentListSortBucket_Declined_IsAlwaysBucket2()
    {
        var invitation = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddDays(1));
        invitation.Decline();

        InterviewRules.GetTalentListSortBucket(invitation, DateTime.UtcNow).Should().Be(2);
    }

    [Fact]
    public void GetTalentListSortBucket_Cancelled_IsAlwaysBucket1_RegardlessOfDate()
    {
        var future = CreateOnlineAssessmentInvitation(DateTime.UtcNow.AddDays(1));
        future.Cancel();

        InterviewRules.GetTalentListSortBucket(future, DateTime.UtcNow).Should().Be(1);
    }

    [Fact]
    public void GetTalentListSortBucket_PendingOrAccepted_IsBucket0UntilPast_ThenBucket1()
    {
        var now = DateTime.UtcNow;
        var upcomingPending = CreateOnlineAssessmentInvitation(now.AddDays(1));
        var expiredPending = CreateOnlineAssessmentInvitation(now.AddDays(-1));
        var upcomingAccepted = CreateOnCallInvitation(now.AddDays(2));
        upcomingAccepted.Accept(upcomingAccepted.Timeslots[0].Id, now.AddDays(2));
        var completedAccepted = CreateOnCallInvitation(now.AddDays(-2));
        completedAccepted.Accept(completedAccepted.Timeslots[0].Id, now.AddDays(-2));

        InterviewRules.GetTalentListSortBucket(upcomingPending, now).Should().Be(0);
        InterviewRules.GetTalentListSortBucket(upcomingAccepted, now).Should().Be(0);
        InterviewRules.GetTalentListSortBucket(expiredPending, now).Should().Be(1);
        InterviewRules.GetTalentListSortBucket(completedAccepted, now).Should().Be(1);
    }
}
