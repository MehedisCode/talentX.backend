using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Commands.UnhideCompanyReview;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.UnhideCompanyReview;

public sealed class UnhideCompanyReviewCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UnhideCompanyReviewCommandHandler _handler;

    public UnhideCompanyReviewCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Companies).Returns(_companyRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.AuditLogs).Returns(_auditLogRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");
        _currentUserService.SetupGet(service => service.Email).Returns("admin@talentx.com");
        _companyRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Company("employer-1", "Acme Inc"));

        _handler = new UnhideCompanyReviewCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static CompanyReview CreateHiddenReview()
    {
        var review = CompanyReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "talent-1", "Backend Engineer",
            ReviewSentiment.Positive, "Great process", 4, 4, 4, 4);
        review.Approve("admin-0");
        review.Hide(ReviewModerationReason.Other, "Some earlier reason for hiding.", "admin-0");
        return review;
    }

    private void SetupReview(CompanyReview review)
    {
        _reviewRepository
            .Setup(repository => repository.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
    }

    [Fact]
    public async Task Handle_WhenReviewIsHidden_UnhidesAndWritesAuditLogInOneSave()
    {
        var review = CreateHiddenReview();
        SetupReview(review);

        var result = await _handler.Handle(new UnhideCompanyReviewCommand(review.Id), CancellationToken.None);

        result.Message.Should().Be("Review unhidden.");
        review.Status.Should().Be(CompanyReviewStatus.Published);
        review.ModerationReason.Should().BeNull();
        review.ModerationDetails.Should().BeNull();
        review.ModeratedByUserId.Should().Be("admin-1");

        _auditLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(log =>
                    log.Actor == "admin@talentx.com" &&
                    log.Action == $"Unhid review {review.Id} for 'Acme Inc' (Backend Engineer)"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReviewIsAlreadyPublished_ThrowsBadRequestAndWritesNothing()
    {
        var review = CompanyReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "talent-1", "Backend Engineer",
            ReviewSentiment.Positive, "Great process", 4, 4, 4, 4);
        review.Approve("admin-0");
        SetupReview(review);

        var handleAction = () => _handler.Handle(new UnhideCompanyReviewCommand(review.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only hidden reviews can be unhidden.");
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

        var handleAction = () => _handler.Handle(new UnhideCompanyReviewCommand(missingId), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "admin@talentx.com")]
    [InlineData("admin-1", null)]
    public async Task Handle_WhenCallerIdentityIsMissing_ThrowsUnauthorizedAndTouchesNothing(string? userId, string? email)
    {
        _currentUserService.SetupGet(service => service.UserId).Returns(userId);
        _currentUserService.SetupGet(service => service.Email).Returns(email);

        var handleAction = () => _handler.Handle(new UnhideCompanyReviewCommand(Guid.NewGuid()), CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to unhide a review.");
        _reviewRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
