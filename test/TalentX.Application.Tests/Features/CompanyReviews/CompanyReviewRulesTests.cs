using FluentAssertions;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.CompanyReviews;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews;

public class CompanyReviewRulesTests
{
    private static readonly DateTime Now = new(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc);

    private const string Admin = "admin-1";

    private static CompanyReview CreateReview() =>
        CompanyReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "talent-1",
            "Backend Engineer",
            ReviewSentiment.Positive,
            "Great process.",
            5,
            4,
            3,
            2);

    private static CompanyReview CreatePublished()
    {
        var review = CreateReview();
        review.Approve(Admin);
        return review;
    }

    private static CompanyReview CreateRejected()
    {
        var review = CreateReview();
        review.Reject(ReviewModerationReason.Spam, "Looks like spam.", Admin);
        return review;
    }

    private static CompanyReview CreateHidden()
    {
        var review = CreatePublished();
        review.Hide(ReviewModerationReason.PersonalInformation, "Names a person.", Admin);
        return review;
    }

    private static CompanyReview CreateWithdrawn()
    {
        var review = CreateReview();
        review.Withdraw();
        return review;
    }

    private static InterviewInvitation NewInvitation(InterviewType type, DateTime? assessmentDeadline = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "employer-1", "talent-1", type, assessmentDeadline: assessmentDeadline);

    private static InterviewInvitation AcceptedAssessment(DateTime? deadline)
    {
        var invitation = NewInvitation(InterviewType.OnlineAssessment, deadline);
        invitation.Accept(null, null);
        return invitation;
    }

    private static InterviewInvitation AcceptedWithSlot(InterviewType type, DateTime bookedEnd)
    {
        var invitation = NewInvitation(type);
        invitation.AddTimeslot(bookedEnd.AddDays(-5), bookedEnd.AddDays(-5).AddHours(1));
        invitation.AddTimeslot(bookedEnd.AddHours(-1), bookedEnd);
        var booked = invitation.Timeslots[1];
        invitation.Accept(booked.Id, booked.StartTime);
        return invitation;
    }

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

    [Fact]
    public void EnsureCanApprove_Pending_DoesNotThrow()
    {
        var act = () => CompanyReviewRules.EnsureCanApprove(CreateReview());

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(NonPendingReviews))]
    public void EnsureCanApprove_NotPending_Throws(Func<CompanyReview> build)
    {
        var act = () => CompanyReviewRules.EnsureCanApprove(build());

        act.Should().Throw<BadRequestException>().WithMessage("Only pending reviews can be approved.");
    }

    [Fact]
    public void EnsureCanReject_Pending_DoesNotThrow()
    {
        var act = () => CompanyReviewRules.EnsureCanReject(CreateReview());

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(NonPendingReviews))]
    public void EnsureCanReject_NotPending_Throws(Func<CompanyReview> build)
    {
        var act = () => CompanyReviewRules.EnsureCanReject(build());

        act.Should().Throw<BadRequestException>().WithMessage("Only pending reviews can be rejected.");
    }

    [Fact]
    public void EnsureCanHide_Published_DoesNotThrow()
    {
        var act = () => CompanyReviewRules.EnsureCanHide(CreatePublished());

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(NonPublishedReviews))]
    public void EnsureCanHide_NotPublished_Throws(Func<CompanyReview> build)
    {
        var act = () => CompanyReviewRules.EnsureCanHide(build());

        act.Should().Throw<BadRequestException>().WithMessage("Only published reviews can be hidden.");
    }

    [Fact]
    public void EnsureCanUnhide_Hidden_DoesNotThrow()
    {
        var act = () => CompanyReviewRules.EnsureCanUnhide(CreateHidden());

        act.Should().NotThrow();
    }

    [Theory]
    [MemberData(nameof(NonHiddenReviews))]
    public void EnsureCanUnhide_NotHidden_Throws(Func<CompanyReview> build)
    {
        var act = () => CompanyReviewRules.EnsureCanUnhide(build());

        act.Should().Throw<BadRequestException>().WithMessage("Only hidden reviews can be unhidden.");
    }

    [Theory]
    [MemberData(nameof(ChangeableReviews))]
    public void EnsureCanBeEdited_ChangeableStatus_DoesNotThrow(Func<CompanyReview> build)
    {
        var act = () => CompanyReviewRules.EnsureCanBeEdited(build());

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanBeEdited_Hidden_ThrowsWithHiddenMessage()
    {
        var act = () => CompanyReviewRules.EnsureCanBeEdited(CreateHidden());

        act.Should().Throw<BadRequestException>().WithMessage("*hidden*");
    }

    [Fact]
    public void EnsureCanBeEdited_Withdrawn_ThrowsWithWithdrawnMessage()
    {
        var act = () => CompanyReviewRules.EnsureCanBeEdited(CreateWithdrawn());

        act.Should().Throw<BadRequestException>().WithMessage("*withdrawn*");
    }

    [Theory]
    [MemberData(nameof(ChangeableReviews))]
    public void EnsureCanBeWithdrawn_ChangeableStatus_DoesNotThrow(Func<CompanyReview> build)
    {
        var act = () => CompanyReviewRules.EnsureCanBeWithdrawn(build());

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanBeWithdrawn_Hidden_ThrowsWithHiddenMessage()
    {
        var act = () => CompanyReviewRules.EnsureCanBeWithdrawn(CreateHidden());

        act.Should().Throw<BadRequestException>().WithMessage("*hidden*");
    }

    [Fact]
    public void EnsureCanBeWithdrawn_AlreadyWithdrawn_ThrowsWithAlreadyWithdrawnMessage()
    {
        var act = () => CompanyReviewRules.EnsureCanBeWithdrawn(CreateWithdrawn());

        act.Should().Throw<BadRequestException>().WithMessage("*already been withdrawn*");
    }

    [Theory]
    [MemberData(nameof(ChangeableReviews))]
    public void CanBeChangedByTalent_PendingPublishedOrRejected_IsTrue(Func<CompanyReview> build)
    {
        CompanyReviewRules.CanBeChangedByTalent(build()).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(LockedReviews))]
    public void CanBeChangedByTalent_HiddenOrWithdrawn_IsFalse(Func<CompanyReview> build)
    {
        CompanyReviewRules.CanBeChangedByTalent(build()).Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void EnsureRatingsInRange_WithinBounds_DoesNotThrow(int rating)
    {
        var act = () => CompanyReviewRules.EnsureRatingsInRange(rating, rating, rating, rating);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureRatingsInRange_AllNull_DoesNotThrow()
    {
        var act = () => CompanyReviewRules.EnsureRatingsInRange(null, null, null, null);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void EnsureRatingsInRange_OutOfRange_ThrowsForEachArea(int rating)
    {
        var cases = new Action[]
        {
            () => CompanyReviewRules.EnsureRatingsInRange(rating, null, null, null),
            () => CompanyReviewRules.EnsureRatingsInRange(null, rating, null, null),
            () => CompanyReviewRules.EnsureRatingsInRange(null, null, rating, null),
            () => CompanyReviewRules.EnsureRatingsInRange(null, null, null, rating)
        };

        foreach (var ensure in cases)
        {
            ensure.Should().Throw<BadRequestException>();
        }
    }

    public static IEnumerable<object[]> NonPendingReviews() => new[]
    {
        new object[] { (Func<CompanyReview>)CreatePublished },
        new object[] { (Func<CompanyReview>)CreateRejected },
        new object[] { (Func<CompanyReview>)CreateHidden },
        new object[] { (Func<CompanyReview>)CreateWithdrawn }
    };

    public static IEnumerable<object[]> NonPublishedReviews() => new[]
    {
        new object[] { (Func<CompanyReview>)(() => CreateReview()) },
        new object[] { (Func<CompanyReview>)CreateRejected },
        new object[] { (Func<CompanyReview>)CreateHidden },
        new object[] { (Func<CompanyReview>)CreateWithdrawn }
    };

    public static IEnumerable<object[]> NonHiddenReviews() => new[]
    {
        new object[] { (Func<CompanyReview>)(() => CreateReview()) },
        new object[] { (Func<CompanyReview>)CreatePublished },
        new object[] { (Func<CompanyReview>)CreateRejected },
        new object[] { (Func<CompanyReview>)CreateWithdrawn }
    };

    public static IEnumerable<object[]> ChangeableReviews() => new[]
    {
        new object[] { (Func<CompanyReview>)(() => CreateReview()) },
        new object[] { (Func<CompanyReview>)CreatePublished },
        new object[] { (Func<CompanyReview>)CreateRejected }
    };

    public static IEnumerable<object[]> LockedReviews() => new[]
    {
        new object[] { (Func<CompanyReview>)CreateHidden },
        new object[] { (Func<CompanyReview>)CreateWithdrawn }
    };
}
