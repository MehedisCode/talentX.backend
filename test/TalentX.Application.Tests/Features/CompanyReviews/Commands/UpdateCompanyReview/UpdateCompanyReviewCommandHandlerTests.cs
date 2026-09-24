using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Commands.UpdateCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.UpdateCompanyReview;

public class UpdateCompanyReviewCommandHandlerTests
{
    private const string TalentId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpdateCompanyReviewCommandHandler _handler;

    public UpdateCompanyReviewCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.CompanyReviews).Returns(_reviewRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentId);

        _handler = new UpdateCompanyReviewCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static CompanyReview NewReview(string talentId = TalentId) =>
        CompanyReview.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), talentId, "Backend Engineer",
            ReviewSentiment.Positive, "Original.", 5, 5, 5, 5);

    private void Returns(CompanyReview review) =>
        _reviewRepository
            .Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

    private static UpdateCompanyReviewCommand Command(
        CompanyReview review,
        ReviewSentiment sentiment = ReviewSentiment.Negative,
        string? comment = "Updated.",
        CompanyReviewRatingsDto? ratings = null) =>
        new(review.Id, sentiment, comment, ratings ?? new CompanyReviewRatingsDto(1, 2, null, 4));

    [Fact]
    public async Task Handle_PendingReview_AppliesTheNewContentAndSaves()
    {
        var review = NewReview();
        Returns(review);

        await _handler.Handle(Command(review), CancellationToken.None);

        review.Status.Should().Be(CompanyReviewStatus.Pending);
        review.Sentiment.Should().Be(ReviewSentiment.Negative);
        review.Comment.Should().Be("Updated.");
        review.RecruitmentProcessRating.Should().Be(1);
        review.CommunicationRating.Should().Be(2);
        review.InterviewExperienceRating.Should().BeNull();
        review.ProfessionalismRating.Should().Be(4);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishedReview_GoesBackToPendingAndLeavesPublicView()
    {
        var review = NewReview();
        review.Approve("admin-1");
        Returns(review);

        await _handler.Handle(Command(review), CancellationToken.None);

        review.Status.Should().Be(CompanyReviewStatus.Pending);
        review.PublishedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RejectedReview_IsResubmittedAsPending()
    {
        var review = NewReview();
        review.Reject(ReviewModerationReason.Spam, "Spam.", "admin-1");
        Returns(review);

        await _handler.Handle(Command(review), CancellationToken.None);

        review.Status.Should().Be(CompanyReviewStatus.Pending);
        review.ModerationReason.Should().BeNull();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  Trim me.  ", "Trim me.")]
    public async Task Handle_NormalizesTheComment(string? input, string? expected)
    {
        var review = NewReview();
        Returns(review);

        await _handler.Handle(Command(review, comment: input), CancellationToken.None);

        review.Comment.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithoutRatings_ClearsAllRatings()
    {
        var review = NewReview();
        Returns(review);
        var command = new UpdateCompanyReviewCommand(review.Id, ReviewSentiment.Positive, "Fine.", null);

        await _handler.Handle(command, CancellationToken.None);

        review.RecruitmentProcessRating.Should().BeNull();
        review.CommunicationRating.Should().BeNull();
        review.InterviewExperienceRating.Should().BeNull();
        review.ProfessionalismRating.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HiddenReview_ThrowsBadRequestAndDoesNotChangeIt()
    {
        var review = NewReview();
        review.Approve("admin-1");
        review.Hide(ReviewModerationReason.Other, "Hidden.", "admin-1");
        Returns(review);

        var act = () => _handler.Handle(Command(review), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        review.Status.Should().Be(CompanyReviewStatus.Hidden);
        review.Comment.Should().Be("Original.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithdrawnReview_ThrowsBadRequest()
    {
        var review = NewReview();
        review.Withdraw();
        Returns(review);

        var act = () => _handler.Handle(Command(review), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotSignedIn_ThrowsUnauthorized()
    {
        var review = NewReview();
        Returns(review);
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(Command(review), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenReviewDoesNotExist_ThrowsNotFound()
    {
        var command = new UpdateCompanyReviewCommand(Guid.NewGuid(), ReviewSentiment.Positive, null, null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenReviewBelongsToAnotherTalent_ThrowsForbiddenAndDoesNotChangeIt()
    {
        var review = NewReview("talent-2");
        Returns(review);

        var act = () => _handler.Handle(Command(review), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        review.Comment.Should().Be("Original.");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
