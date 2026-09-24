using FluentAssertions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public class CompanyReviewTests
{
    private const string Admin = "admin-1";

    private static CompanyReview CreateReview(
        ReviewSentiment sentiment = ReviewSentiment.Positive,
        string? comment = "Great process.",
        int? recruitment = 5,
        int? communication = 4,
        int? interview = 3,
        int? professionalism = 2) =>
        CompanyReview.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "talent-1",
            "Backend Engineer",
            sentiment,
            comment,
            recruitment,
            communication,
            interview,
            professionalism);

    private static void EditReview(
        CompanyReview review,
        ReviewSentiment sentiment = ReviewSentiment.Positive,
        string? comment = "Great process.",
        int? recruitment = 5,
        int? communication = 4,
        int? interview = 3,
        int? professionalism = 2) =>
        review.Edit(sentiment, comment, recruitment, communication, interview, professionalism);

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

    // ---- Create ----

    [Fact]
    public void Create_SetsAllFieldsFromArguments()
    {
        var companyId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        var review = CompanyReview.Create(
            companyId,
            jobId,
            applicationId,
            "talent-1",
            "Backend Engineer",
            ReviewSentiment.Negative,
            "Slow replies.",
            1,
            null,
            3,
            null);

        review.Id.Should().NotBeEmpty();
        review.CompanyId.Should().Be(companyId);
        review.JobId.Should().Be(jobId);
        review.JobApplicationId.Should().Be(applicationId);
        review.TalentUserId.Should().Be("talent-1");
        review.JobTitle.Should().Be("Backend Engineer");
        review.Sentiment.Should().Be(ReviewSentiment.Negative);
        review.Comment.Should().Be("Slow replies.");
        review.RecruitmentProcessRating.Should().Be(1);
        review.CommunicationRating.Should().BeNull();
        review.InterviewExperienceRating.Should().Be(3);
        review.ProfessionalismRating.Should().BeNull();
    }

    [Fact]
    public void Create_StartsPendingWithSubmittedAtAndNoModerationData()
    {
        var before = DateTime.UtcNow;
        var review = CreateReview();
        var after = DateTime.UtcNow;

        review.Status.Should().Be(CompanyReviewStatus.Pending);
        review.SubmittedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        review.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        review.UpdatedAt.Should().BeNull();
        review.PublishedAt.Should().BeNull();
        review.WithdrawnAt.Should().BeNull();
        review.ModerationReason.Should().BeNull();
        review.ModerationDetails.Should().BeNull();
        review.ModeratedByUserId.Should().BeNull();
        review.ModeratedAt.Should().BeNull();
    }

    [Fact]
    public void Create_AllowsAllRatingsAndCommentToBeNull()
    {
        var review = CreateReview(comment: null, recruitment: null, communication: null, interview: null, professionalism: null);

        review.Comment.Should().BeNull();
        review.RecruitmentProcessRating.Should().BeNull();
        review.CommunicationRating.Should().BeNull();
        review.InterviewExperienceRating.Should().BeNull();
        review.ProfessionalismRating.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Create_AcceptsRatingBounds(int rating)
    {
        var act = () => CreateReview(recruitment: rating, communication: rating, interview: rating, professionalism: rating);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_RatingOutOfRange_ThrowsForEachArea(int rating)
    {
        var cases = new Action[]
        {
            () => CreateReview(recruitment: rating),
            () => CreateReview(communication: rating),
            () => CreateReview(interview: rating),
            () => CreateReview(professionalism: rating)
        };

        foreach (var create in cases)
        {
            create.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    // ---- Edit ----

    [Fact]
    public void Edit_Pending_ReplacesContentAndStaysPending()
    {
        var review = CreateReview();

        EditReview(review, ReviewSentiment.Negative, "Changed my mind.", null, 1, null, 5);

        review.Status.Should().Be(CompanyReviewStatus.Pending);
        review.Sentiment.Should().Be(ReviewSentiment.Negative);
        review.Comment.Should().Be("Changed my mind.");
        review.RecruitmentProcessRating.Should().BeNull();
        review.CommunicationRating.Should().Be(1);
        review.InterviewExperienceRating.Should().BeNull();
        review.ProfessionalismRating.Should().Be(5);
        review.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Edit_Published_ReturnsToPendingAndClearsPublishedAndModeration()
    {
        var review = CreatePublished();
        review.PublishedAt.Should().NotBeNull();

        EditReview(review, comment: "Updated.");

        review.Status.Should().Be(CompanyReviewStatus.Pending);
        review.PublishedAt.Should().BeNull();
        review.ModeratedAt.Should().BeNull();
        review.ModeratedByUserId.Should().BeNull();
        review.ModerationReason.Should().BeNull();
        review.ModerationDetails.Should().BeNull();
        review.Comment.Should().Be("Updated.");
    }

    [Fact]
    public void Edit_Rejected_ResubmitsAsPendingAndClearsRejectionReason()
    {
        var review = CreateRejected();
        review.ModerationReason.Should().Be(ReviewModerationReason.Spam);

        EditReview(review, comment: "Rewritten.");

        review.Status.Should().Be(CompanyReviewStatus.Pending);
        review.ModerationReason.Should().BeNull();
        review.ModerationDetails.Should().BeNull();
        review.ModeratedByUserId.Should().BeNull();
        review.ModeratedAt.Should().BeNull();
    }

    [Fact]
    public void Edit_Hidden_ThrowsAndLeavesReviewUntouched()
    {
        var review = CreateHidden();

        var act = () => EditReview(review, comment: "Sneaky.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*hidden*");
        review.Status.Should().Be(CompanyReviewStatus.Hidden);
        review.Comment.Should().Be("Great process.");
    }

    [Fact]
    public void Edit_Withdrawn_Throws()
    {
        var review = CreateWithdrawn();

        var act = () => EditReview(review);

        act.Should().Throw<InvalidOperationException>().WithMessage("*withdrawn*");
        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
    }

    [Fact]
    public void Edit_RatingOutOfRange_ThrowsAndLeavesReviewUntouched()
    {
        var review = CreatePublished();

        var act = () => EditReview(review, comment: "Nope.", communication: 9);

        act.Should().Throw<ArgumentOutOfRangeException>();
        review.Status.Should().Be(CompanyReviewStatus.Published);
        review.Comment.Should().Be("Great process.");
    }

    // ---- Withdraw ----

    [Fact]
    public void Withdraw_Pending_SetsWithdrawnAndTimestamps()
    {
        var review = CreateReview();
        var before = DateTime.UtcNow;

        review.Withdraw();

        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
        review.WithdrawnAt.Should().NotBeNull();
        review.WithdrawnAt!.Value.Should().BeOnOrAfter(before);
        review.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Withdraw_Published_Succeeds()
    {
        var review = CreatePublished();

        review.Withdraw();

        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_Rejected_Succeeds()
    {
        var review = CreateRejected();

        review.Withdraw();

        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_Hidden_Throws()
    {
        var review = CreateHidden();

        var act = () => review.Withdraw();

        act.Should().Throw<InvalidOperationException>().WithMessage("*hidden*");
        review.Status.Should().Be(CompanyReviewStatus.Hidden);
        review.WithdrawnAt.Should().BeNull();
    }

    [Fact]
    public void Withdraw_AlreadyWithdrawn_Throws()
    {
        var review = CreateWithdrawn();

        var act = () => review.Withdraw();

        act.Should().Throw<InvalidOperationException>().WithMessage("*already been withdrawn*");
    }

    [Fact]
    public void Withdrawn_IsFinal_NoTransitionSucceeds()
    {
        var review = CreateWithdrawn();

        var actions = new Action[]
        {
            () => EditReview(review),
            () => review.Withdraw(),
            () => review.Approve(Admin),
            () => review.Reject(ReviewModerationReason.Other, "x", Admin),
            () => review.Hide(ReviewModerationReason.Other, "x", Admin),
            () => review.Unhide(Admin)
        };

        foreach (var action in actions)
        {
            action.Should().Throw<InvalidOperationException>();
        }

        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
    }

    // ---- Approve ----

    [Fact]
    public void Approve_Pending_PublishesAndRecordsModerator()
    {
        var review = CreateReview();
        var before = DateTime.UtcNow;

        review.Approve(Admin);

        review.Status.Should().Be(CompanyReviewStatus.Published);
        review.PublishedAt.Should().NotBeNull();
        review.PublishedAt!.Value.Should().BeOnOrAfter(before);
        review.ModeratedByUserId.Should().Be(Admin);
        review.ModeratedAt.Should().NotBeNull();
        review.ModerationReason.Should().BeNull();
        review.ModerationDetails.Should().BeNull();
        review.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_ReEditedAfterRejection_ClearsOldReasonWhenPublished()
    {
        var review = CreateRejected();
        EditReview(review, comment: "Better.");

        review.Approve(Admin);

        review.Status.Should().Be(CompanyReviewStatus.Published);
        review.ModerationReason.Should().BeNull();
        review.ModerationDetails.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(NonPendingReviews))]
    public void Approve_NotPending_Throws(Func<CompanyReview> build)
    {
        var review = build();
        var status = review.Status;

        var act = () => review.Approve(Admin);

        act.Should().Throw<InvalidOperationException>().WithMessage("Only pending reviews can be approved.");
        review.Status.Should().Be(status);
    }

    // ---- Reject ----

    [Fact]
    public void Reject_Pending_RecordsReasonDetailsAndModerator()
    {
        var review = CreateReview();

        review.Reject(ReviewModerationReason.FalseOrMisleading, "Claims are unverifiable.", Admin);

        review.Status.Should().Be(CompanyReviewStatus.Rejected);
        review.ModerationReason.Should().Be(ReviewModerationReason.FalseOrMisleading);
        review.ModerationDetails.Should().Be("Claims are unverifiable.");
        review.ModeratedByUserId.Should().Be(Admin);
        review.ModeratedAt.Should().NotBeNull();
        review.PublishedAt.Should().BeNull();
        review.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(NonPendingReviews))]
    public void Reject_NotPending_Throws(Func<CompanyReview> build)
    {
        var review = build();
        var status = review.Status;

        var act = () => review.Reject(ReviewModerationReason.Spam, "x", Admin);

        act.Should().Throw<InvalidOperationException>().WithMessage("Only pending reviews can be rejected.");
        review.Status.Should().Be(status);
    }

    // ---- Hide ----

    [Fact]
    public void Hide_Published_RecordsReasonAndKeepsPublishedAt()
    {
        var review = CreatePublished();
        var publishedAt = review.PublishedAt;

        review.Hide(ReviewModerationReason.InappropriateContent, "Abusive language.", Admin);

        review.Status.Should().Be(CompanyReviewStatus.Hidden);
        review.ModerationReason.Should().Be(ReviewModerationReason.InappropriateContent);
        review.ModerationDetails.Should().Be("Abusive language.");
        review.ModeratedByUserId.Should().Be(Admin);
        review.PublishedAt.Should().Be(publishedAt);
    }

    [Theory]
    [MemberData(nameof(NonPublishedReviews))]
    public void Hide_NotPublished_Throws(Func<CompanyReview> build)
    {
        var review = build();
        var status = review.Status;

        var act = () => review.Hide(ReviewModerationReason.Spam, "x", Admin);

        act.Should().Throw<InvalidOperationException>().WithMessage("Only published reviews can be hidden.");
        review.Status.Should().Be(status);
    }

    // ---- Unhide ----

    [Fact]
    public void Unhide_Hidden_RepublishesAndClearsReason()
    {
        var review = CreateHidden();

        review.Unhide("admin-2");

        review.Status.Should().Be(CompanyReviewStatus.Published);
        review.ModerationReason.Should().BeNull();
        review.ModerationDetails.Should().BeNull();
        review.ModeratedByUserId.Should().Be("admin-2");
        review.PublishedAt.Should().NotBeNull();
        review.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(NonHiddenReviews))]
    public void Unhide_NotHidden_Throws(Func<CompanyReview> build)
    {
        var review = build();
        var status = review.Status;

        var act = () => review.Unhide(Admin);

        act.Should().Throw<InvalidOperationException>().WithMessage("Only hidden reviews can be unhidden.");
        review.Status.Should().Be(status);
    }

    // ---- CanBeChangedByTalent ----

    [Theory]
    [MemberData(nameof(ChangeableReviews))]
    public void CanBeChangedByTalent_PendingPublishedOrRejected_IsTrue(Func<CompanyReview> build)
    {
        build().CanBeChangedByTalent.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(LockedReviews))]
    public void CanBeChangedByTalent_HiddenOrWithdrawn_IsFalse(Func<CompanyReview> build)
    {
        build().CanBeChangedByTalent.Should().BeFalse();
    }

    // ---- Shared ----

    [Fact]
    public void EveryLegalTransition_SetsUpdatedAt()
    {
        var transitions = new Func<CompanyReview>[]
        {
            () => { var r = CreateReview(); EditReview(r); return r; },
            () => { var r = CreateReview(); r.Withdraw(); return r; },
            () => { var r = CreateReview(); r.Approve(Admin); return r; },
            () => { var r = CreateReview(); r.Reject(ReviewModerationReason.Spam, "x", Admin); return r; },
            () => { var r = CreatePublished(); r.Hide(ReviewModerationReason.Spam, "x", Admin); return r; },
            () => { var r = CreateHidden(); r.Unhide(Admin); return r; }
        };

        foreach (var transition in transitions)
        {
            transition().UpdatedAt.Should().NotBeNull();
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
