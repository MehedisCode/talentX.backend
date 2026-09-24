using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.WithdrawCompanyReview;

public class WithdrawCompanyReviewCommandHandlerTests
{
    private const string TalentId = "talent-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly WithdrawCompanyReviewCommandHandler _handler;

    public WithdrawCompanyReviewCommandHandlerTests()
    {
        _unitOfWork.SetupGet(u => u.CompanyReviews).Returns(_reviewRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns(TalentId);

        _handler = new WithdrawCompanyReviewCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static CompanyReview NewReview(string talentId = TalentId) =>
        CompanyReview.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), talentId, "Backend Engineer",
            ReviewSentiment.Positive, "Original.", 5, 5, 5, 5);

    private void Returns(CompanyReview review) =>
        _reviewRepository
            .Setup(r => r.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

    [Fact]
    public async Task Handle_PendingReview_IsWithdrawnAndSaved()
    {
        var review = NewReview();
        Returns(review);

        await _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
        review.WithdrawnAt.Should().NotBeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTheConfirmationMessage()
    {
        var review = NewReview();
        Returns(review);

        var result = await _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        result.Message.Should().Be("Review withdrawn.");
    }

    [Fact]
    public async Task Handle_PublishedReview_IsWithdrawn()
    {
        var review = NewReview();
        review.Approve("admin-1");
        Returns(review);

        await _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
    }

    [Fact]
    public async Task Handle_RejectedReview_IsWithdrawn()
    {
        var review = NewReview();
        review.Reject(ReviewModerationReason.Spam, "Spam.", "admin-1");
        Returns(review);

        await _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        review.Status.Should().Be(CompanyReviewStatus.Withdrawn);
    }

    [Fact]
    public async Task Handle_HiddenReview_ThrowsBadRequest_SoATakedownCannotBeDodged()
    {
        var review = NewReview();
        review.Approve("admin-1");
        review.Hide(ReviewModerationReason.Other, "Hidden.", "admin-1");
        Returns(review);

        var act = () => _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        review.Status.Should().Be(CompanyReviewStatus.Hidden);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyWithdrawnReview_ThrowsBadRequest_WithdrawIsFinal()
    {
        var review = NewReview();
        review.Withdraw();
        Returns(review);

        var act = () => _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotSignedIn_ThrowsUnauthorized()
    {
        var review = NewReview();
        Returns(review);
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenReviewDoesNotExist_ThrowsNotFound()
    {
        var act = () => _handler.Handle(new WithdrawCompanyReviewCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenReviewBelongsToAnotherTalent_ThrowsForbiddenAndDoesNotChangeIt()
    {
        var review = NewReview("talent-2");
        Returns(review);

        var act = () => _handler.Handle(new WithdrawCompanyReviewCommand(review.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        review.Status.Should().Be(CompanyReviewStatus.Pending);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
