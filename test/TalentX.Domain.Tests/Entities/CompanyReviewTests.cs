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
}
