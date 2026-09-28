using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Commands.RejectCompanyReview;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.RejectCompanyReview;

public sealed class RejectCompanyReviewCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly RejectCompanyReviewCommandHandler _handler;

    public RejectCompanyReviewCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Companies).Returns(_companyRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.AuditLogs).Returns(_auditLogRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");
        _currentUserService.SetupGet(service => service.Email).Returns("admin@talentx.com");
        _companyRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Company("employer-1", "Acme Inc"));

        _handler = new RejectCompanyReviewCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static CompanyReview CreateReview()
    {
        return CompanyReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "talent-1", "Backend Engineer",
            ReviewSentiment.Negative, "Bad process", null, null, null, null);
    }

    private void SetupReview(CompanyReview review)
    {
        _reviewRepository
            .Setup(repository => repository.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
    }

    private static RejectCompanyReviewCommand Command(
        Guid reviewId,
        ReviewModerationReason reason = ReviewModerationReason.Spam,
        string details = "This looks like a spam submission.")
    {
        return new RejectCompanyReviewCommand(reviewId, reason, details);
    }

    [Fact]
    public async Task Handle_WhenReviewIsPending_RejectsAndWritesAuditLogInOneSave()
    {
        var review = CreateReview();
        SetupReview(review);

        var result = await _handler.Handle(Command(review.Id), CancellationToken.None);

        result.Message.Should().Be("Review rejected.");
        review.Status.Should().Be(CompanyReviewStatus.Rejected);
        review.ModerationReason.Should().Be(ReviewModerationReason.Spam);
        review.ModerationDetails.Should().Be("This looks like a spam submission.");
        review.ModeratedByUserId.Should().Be("admin-1");

        _auditLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(log =>
                    log.Actor == "admin@talentx.com" &&
                    log.Action == $"Rejected review {review.Id} for 'Acme Inc' (Backend Engineer) (Spam)"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrimsDetailsBeforeStoringThem()
    {
        var review = CreateReview();
        SetupReview(review);

        await _handler.Handle(Command(review.Id, details: "   Contains a phone number.   "), CancellationToken.None);

        review.ModerationDetails.Should().Be("Contains a phone number.");
    }

    [Fact]
    public async Task Handle_WhenReviewIsAlreadyHidden_ThrowsBadRequestAndWritesNothing()
    {
        var review = CreateReview();
        review.Approve("admin-0");
        review.Hide(ReviewModerationReason.Other, "Some earlier reason for hiding.", "admin-0");
        SetupReview(review);

        var handleAction = () => _handler.Handle(Command(review.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only pending reviews can be rejected.");
        _auditLogRepository.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenReviewDoesNotExist_ThrowsNotFoundAndDoesNotSave()
    {
        var missingId = Guid.NewGuid();
        _reviewRepository
            .Setup(repository => repository.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyReview?)null);

        var handleAction = () => _handler.Handle(Command(missingId), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
