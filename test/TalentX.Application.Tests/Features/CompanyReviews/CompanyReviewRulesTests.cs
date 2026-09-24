using FluentAssertions;
using TalentX.Application.Features.CompanyReviews;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews;

public class CompanyReviewRulesTests
{
    private static readonly DateTime Now = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

    private static InterviewInvitation NewInvitation(InterviewType type, DateTime? assessmentDeadline = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "employer-1", "talent-1", type, assessmentDeadline: assessmentDeadline);

    private static InterviewInvitation AcceptedAssessment(DateTime? deadline)
    {
        var invitation = NewInvitation(InterviewType.OnlineAssessment, deadline);
        invitation.Accept(null, null);
        return invitation;
    }

    // Adds a decoy slot first so a test can tell the booked slot from the others.
    private static InterviewInvitation AcceptedWithSlot(InterviewType type, DateTime bookedEnd)
    {
        var invitation = NewInvitation(type);
        invitation.AddTimeslot(bookedEnd.AddDays(-5), bookedEnd.AddDays(-5).AddHours(1));
        invitation.AddTimeslot(bookedEnd.AddHours(-1), bookedEnd);
        var booked = invitation.Timeslots[1];
        invitation.Accept(booked.Id, booked.StartTime);
        return invitation;
    }

    // ---- NormalizeComment ----

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Great team.  ", "Great team.")]
    [InlineData("Line one\nLine two", "Line one\nLine two")]
    public void NormalizeComment_TrimsAndTurnsBlankIntoNull(string? input, string? expected)
    {
        CompanyReviewRules.NormalizeComment(input).Should().Be(expected);
    }

    // ---- TryGetUnlockAt ----

    [Theory]
    [InlineData(InterviewType.OnCall)]
    [InlineData(InterviewType.OnSite)]
    public void TryGetUnlockAt_SlotBasedInterview_ReturnsBookedSlotEndTime(InterviewType type)
    {
        var end = Now.AddDays(3);
        var invitation = AcceptedWithSlot(type, end);

        CompanyReviewRules.TryGetUnlockAt(invitation, assessmentDelayDays: 1).Should().Be(end);
    }

    [Theory]
    [InlineData(InterviewType.OnCall)]
    [InlineData(InterviewType.OnSite)]
    public void TryGetUnlockAt_SlotBasedInterview_IgnoresAssessmentDelay(InterviewType type)
    {
        var end = Now.AddDays(3);
        var invitation = AcceptedWithSlot(type, end);

        CompanyReviewRules.TryGetUnlockAt(invitation, assessmentDelayDays: 7).Should().Be(end);
    }

    [Theory]
    [InlineData(0, 23)]
    [InlineData(1, 24)]
    [InlineData(2, 25)]
    public void TryGetUnlockAt_OnlineAssessment_AddsGraceDaysToDeadline(int delayDays, int expectedDay)
    {
        var deadline = new DateTime(2026, 9, 23, 17, 30, 0, DateTimeKind.Utc);
        var invitation = AcceptedAssessment(deadline);

        var unlockAt = CompanyReviewRules.TryGetUnlockAt(invitation, delayDays);

        unlockAt.Should().Be(new DateTime(2026, 9, expectedDay, 17, 30, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(InterviewType.OnCall)]
    [InlineData(InterviewType.OnSite)]
    public void TryGetUnlockAt_SlotBasedWithNoTimeslots_ReturnsNull(InterviewType type)
    {
        var invitation = NewInvitation(type);
        invitation.Accept(null, null);

        CompanyReviewRules.TryGetUnlockAt(invitation, 1).Should().BeNull();
    }

    [Fact]
    public void TryGetUnlockAt_SlotBasedWithSlotsButNoneSelected_ReturnsNull()
    {
        var invitation = NewInvitation(InterviewType.OnCall);
        invitation.AddTimeslot(Now, Now.AddHours(1));
        invitation.Accept(null, null);

        CompanyReviewRules.TryGetUnlockAt(invitation, 1).Should().BeNull();
    }

    [Fact]
    public void TryGetUnlockAt_SelectedSlotIdMatchesNoTimeslot_ReturnsNull()
    {
        var invitation = NewInvitation(InterviewType.OnSite);
        invitation.AddTimeslot(Now, Now.AddHours(1));
        invitation.Accept(Guid.NewGuid(), Now);

        CompanyReviewRules.TryGetUnlockAt(invitation, 1).Should().BeNull();
    }

    [Fact]
    public void TryGetUnlockAt_AssessmentWithNoDeadline_ReturnsNull()
    {
        CompanyReviewRules.TryGetUnlockAt(AcceptedAssessment(null), 1).Should().BeNull();
    }

    [Fact]
    public void TryGetUnlockAt_PendingInvitation_ReturnsNull()
    {
        var invitation = NewInvitation(InterviewType.OnlineAssessment, Now.AddDays(-2));

        CompanyReviewRules.TryGetUnlockAt(invitation, 1).Should().BeNull();
    }

    [Fact]
    public void TryGetUnlockAt_DeclinedInvitation_ReturnsNull()
    {
        var invitation = NewInvitation(InterviewType.OnlineAssessment, Now.AddDays(-2));
        invitation.Decline();

        CompanyReviewRules.TryGetUnlockAt(invitation, 1).Should().BeNull();
    }

    [Fact]
    public void TryGetUnlockAt_CancelledInvitation_ReturnsNull()
    {
        var invitation = AcceptedAssessment(Now.AddDays(-2));
        invitation.Cancel();

        CompanyReviewRules.TryGetUnlockAt(invitation, 1).Should().BeNull();
    }

    // ---- TryGetApplicationUnlockAt ----

    [Fact]
    public void TryGetApplicationUnlockAt_NoInvitations_ReturnsNull()
    {
        CompanyReviewRules.TryGetApplicationUnlockAt(Array.Empty<InterviewInvitation>(), 1).Should().BeNull();
    }

    [Fact]
    public void TryGetApplicationUnlockAt_SingleRound_ReturnsThatRoundsUnlock()
    {
        var end = Now.AddDays(1);

        var unlockAt = CompanyReviewRules.TryGetApplicationUnlockAt(new[] { AcceptedWithSlot(InterviewType.OnCall, end) }, 1);

        unlockAt.Should().Be(end);
    }

    [Fact]
    public void TryGetApplicationUnlockAt_MultipleRounds_ReturnsTheEarliest()
    {
        var early = Now.AddDays(-3);
        var late = Now.AddDays(10);

        var unlockAt = CompanyReviewRules.TryGetApplicationUnlockAt(
            new[]
            {
                AcceptedWithSlot(InterviewType.OnSite, late),
                AcceptedWithSlot(InterviewType.OnCall, early)
            },
            1);

        unlockAt.Should().Be(early);
    }

    [Fact]
    public void TryGetApplicationUnlockAt_LaterNotYetEndedRound_DoesNotRelockAnEndedOne()
    {
        var ended = Now.AddDays(-2);
        var upcoming = Now.AddDays(5);

        var unlockAt = CompanyReviewRules.TryGetApplicationUnlockAt(
            new[]
            {
                AcceptedWithSlot(InterviewType.OnCall, ended),
                AcceptedWithSlot(InterviewType.OnSite, upcoming)
            },
            1);

        CompanyReviewRules.IsUnlocked(unlockAt, Now).Should().BeTrue();
    }

    [Fact]
    public void TryGetApplicationUnlockAt_IgnoresRoundWithNoDerivableTime()
    {
        var end = Now.AddDays(2);
        var noSlot = NewInvitation(InterviewType.OnCall);
        noSlot.Accept(null, null);

        var unlockAt = CompanyReviewRules.TryGetApplicationUnlockAt(
            new[] { noSlot, AcceptedWithSlot(InterviewType.OnSite, end) },
            1);

        unlockAt.Should().Be(end);
    }

    [Fact]
    public void TryGetApplicationUnlockAt_NoRoundYieldsATime_ReturnsNull()
    {
        var noDeadline = AcceptedAssessment(null);
        var pending = NewInvitation(InterviewType.OnlineAssessment, Now);

        CompanyReviewRules.TryGetApplicationUnlockAt(new[] { noDeadline, pending }, 1).Should().BeNull();
    }

    [Fact]
    public void TryGetApplicationUnlockAt_MixesAssessmentAndSlotRounds()
    {
        var deadline = Now.AddDays(-1);
        var slotEnd = Now.AddDays(4);

        var unlockAt = CompanyReviewRules.TryGetApplicationUnlockAt(
            new[] { AcceptedWithSlot(InterviewType.OnCall, slotEnd), AcceptedAssessment(deadline) },
            2);

        unlockAt.Should().Be(deadline.AddDays(2));
    }

    // ---- IsUnlocked ----

    [Fact]
    public void IsUnlocked_NullUnlockAt_IsLocked()
    {
        CompanyReviewRules.IsUnlocked(null, Now).Should().BeFalse();
    }

    [Fact]
    public void IsUnlocked_OneTickBeforeUnlock_IsLocked()
    {
        CompanyReviewRules.IsUnlocked(Now.AddTicks(1), Now).Should().BeFalse();
    }

    [Fact]
    public void IsUnlocked_ExactlyAtUnlock_IsUnlocked()
    {
        CompanyReviewRules.IsUnlocked(Now, Now).Should().BeTrue();
    }

    [Fact]
    public void IsUnlocked_AfterUnlock_IsUnlocked()
    {
        CompanyReviewRules.IsUnlocked(Now.AddMinutes(-1), Now).Should().BeTrue();
    }

    [Fact]
    public void IsUnlocked_AssessmentBoundary_DeadlinePlusGraceDays()
    {
        var deadline = new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc);
        var unlockAt = CompanyReviewRules.TryGetUnlockAt(AcceptedAssessment(deadline), assessmentDelayDays: 2);

        CompanyReviewRules.IsUnlocked(unlockAt, new DateTime(2026, 9, 24, 23, 59, 59, DateTimeKind.Utc)).Should().BeFalse();
        CompanyReviewRules.IsUnlocked(unlockAt, new DateTime(2026, 9, 25, 0, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
    }
}
