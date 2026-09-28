using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewDetails;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetAdminCompanyReviewDetails;

public sealed class GetAdminCompanyReviewDetailsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GetAdminCompanyReviewDetailsQueryHandler _handler;

    public GetAdminCompanyReviewDetailsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Companies).Returns(_companyRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_userRepository.Object);
        _handler = new GetAdminCompanyReviewDetailsQueryHandler(_unitOfWork.Object);
    }

    private static CompanyReview MakeReview(Guid companyId, string talentUserId = "talent-1")
    {
        return CompanyReview.Create(
            companyId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            talentUserId,
            "Backend Engineer",
            ReviewSentiment.Positive,
            "Great process",
            4, 4, 4, 4);
    }

    private void SetupReview(CompanyReview review)
    {
        _reviewRepository
            .Setup(repository => repository.GetByIdAsync(review.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
    }

    [Fact]
    public async Task Handle_ReturnsFullDetailsWithReviewerAndCompany()
    {
        var companyId = Guid.NewGuid();
        var review = MakeReview(companyId, "talent-1");
        SetupReview(review);
        _companyRepository
            .Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Company("employer-1", "Acme Inc"));
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Talent("talent-1", "Jordan Smith", "jordan.smith@ait.com"));

        var result = await _handler.Handle(new GetAdminCompanyReviewDetailsQuery(review.Id), CancellationToken.None);

        result.Id.Should().Be(review.Id);
        result.CompanyName.Should().Be("Acme Inc");
        result.ReviewerName.Should().Be("Jordan Smith");
        result.ReviewerEmail.Should().Be("jordan.smith@ait.com");
        result.ReviewerUserId.Should().NotBeNull();
        result.ModeratedByEmail.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenReviewerAccountIsDeleted_ShowsDeletedUserWithNullId()
    {
        var companyId = Guid.NewGuid();
        var review = MakeReview(companyId, "gone-talent");
        SetupReview(review);
        _companyRepository
            .Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Company("employer-1", "Acme Inc"));
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("gone-talent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new GetAdminCompanyReviewDetailsQuery(review.Id), CancellationToken.None);

        result.ReviewerName.Should().Be("Deleted user");
        result.ReviewerEmail.Should().BeEmpty();
        result.ReviewerUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ResolvesModeratorEmailWhenReviewWasModerated()
    {
        var companyId = Guid.NewGuid();
        var review = MakeReview(companyId);
        review.Reject(ReviewModerationReason.Spam, "This looks like spam content.", "admin-1");
        SetupReview(review);
        _companyRepository
            .Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Company("employer-1", "Acme Inc"));
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Talent("talent-1", "Jordan Smith", "jordan.smith@ait.com"));
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("admin-1", "Admin One", "admin1@talentx.com", UserRole.Admin, isVerified: true));

        var result = await _handler.Handle(new GetAdminCompanyReviewDetailsQuery(review.Id), CancellationToken.None);

        result.ModerationReason.Should().Be(ReviewModerationReason.Spam);
        result.ModerationDetails.Should().Be("This looks like spam content.");
        result.ModeratedByEmail.Should().Be("admin1@talentx.com");
    }

    [Fact]
    public async Task Handle_WhenReviewDoesNotExist_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _reviewRepository
            .Setup(repository => repository.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyReview?)null);

        var handleAction = () => _handler.Handle(new GetAdminCompanyReviewDetailsQuery(missingId), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenCompanyRecordIsMissing_ReturnsEmptyCompanyName()
    {
        var companyId = Guid.NewGuid();
        var review = MakeReview(companyId);
        SetupReview(review);
        _companyRepository
            .Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Talent("talent-1", "Jordan Smith", "jordan.smith@ait.com"));

        var result = await _handler.Handle(new GetAdminCompanyReviewDetailsQuery(review.Id), CancellationToken.None);

        result.CompanyName.Should().BeEmpty();
    }
}
