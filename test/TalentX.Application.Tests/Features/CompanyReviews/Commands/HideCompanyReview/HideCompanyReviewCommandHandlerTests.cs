using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Commands.HideCompanyReview;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.HideCompanyReview;

public sealed class HideCompanyReviewCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly HideCompanyReviewCommandHandler _handler;

    public HideCompanyReviewCommandHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Companies).Returns(_companyRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.AuditLogs).Returns(_auditLogRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns("admin-1");
        _currentUserService.SetupGet(service => service.Email).Returns("admin@talentx.com");
        _companyRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Company("employer-1", "Acme Inc"));

        _handler = new HideCompanyReviewCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static CompanyReview CreatePublishedReview()
    {
        var review = CompanyReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "talent-1", "Backend Engineer",
            ReviewSentiment.Positive, "Great process", 4, 4, 4, 4);
        review.Approve("admin-0");
        return review;
    }

    private void SetupReview(CompanyReview review)
    {
        _reviewRepository
            .Setup(repository => repository.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
    }

    private static HideCompanyReviewCommand Command(
        Guid reviewId,
        ReviewModerationReason reason = ReviewModerationReason.PersonalInformation,
        string details = "Contains the employer's home address.")
    {
        return new HideCompanyReviewCommand(reviewId, reason, details);
    }

    [Fact]
    public async Task Handle_WhenReviewIsPublished_HidesAndWritesAuditLogInOneSave()
    {
        var review = CreatePublishedReview();
        SetupReview(review);

        var result = await _handler.Handle(Command(review.Id), CancellationToken.None);

        result.Message.Should().Be("Review hidden.");
        review.Status.Should().Be(CompanyReviewStatus.Hidden);
        review.ModerationReason.Should().Be(ReviewModerationReason.PersonalInformation);
        review.ModerationDetails.Should().Be("Contains the employer's home address.");
        review.ModeratedByUserId.Should().Be("admin-1");

        _auditLogRepository.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(log =>
                    log.Actor == "admin@talentx.com" &&
                    log.Action == $"Hid review {review.Id} for 'Acme Inc' (Backend Engineer) (Personal information)"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TrimsDetailsBeforeStoringThem()
    {
        var review = CreatePublishedReview();
        SetupReview(review);

        await _handler.Handle(Command(review.Id, details: "   Reveals a home address.   "), CancellationToken.None);

        review.ModerationDetails.Should().Be("Reveals a home address.");
    }

    [Fact]
    public async Task Handle_WhenReviewIsPending_ThrowsBadRequestAndWritesNothing()
    {
        var review = CompanyReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "talent-1", "Backend Engineer",
            ReviewSentiment.Positive, "Great process", 4, 4, 4, 4);
        SetupReview(review);

        var handleAction = () => _handler.Handle(Command(review.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only published reviews can be hidden.");
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
