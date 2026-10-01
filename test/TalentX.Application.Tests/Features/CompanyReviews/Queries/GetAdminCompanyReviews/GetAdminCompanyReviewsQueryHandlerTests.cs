using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviews;
using TalentX.Domain;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetAdminCompanyReviews;

public sealed class GetAdminCompanyReviewsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly GetAdminCompanyReviewsQueryHandler _handler;

    public GetAdminCompanyReviewsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_userRepository.Object);
        _userRepository
            .Setup(repository => repository.GetByUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _handler = new GetAdminCompanyReviewsQueryHandler(_unitOfWork.Object);
    }

    private static AdminCompanyReviewListItemDto MakeItem(string talentUserId = "talent-1", CompanyReviewStatus status = CompanyReviewStatus.Pending)
    {
        return new AdminCompanyReviewListItemDto
        {
            Id = Guid.NewGuid(),
            Status = status,
            Ratings = new CompanyReviewRatingsDto(null, null, null, null),
            CompanyName = "Acme Inc",
            JobTitle = "Backend Engineer",
            TalentUserId = talentUserId
        };
    }

    [Fact]
    public async Task Handle_WithDefaultQuery_NormalizesPaginationAndPassesFilterToRepository()
    {
        AdminCompanyReviewFilter? capturedFilter = null;
        _reviewRepository
            .Setup(repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 1, 20, It.IsAny<CancellationToken>()))
            .Callback<AdminCompanyReviewFilter, int, int, CancellationToken>((filter, _, _, _) => capturedFilter = filter)
            .ReturnsAsync(new PaginatedList<AdminCompanyReviewListItemDto>(new List<AdminCompanyReviewListItemDto>(), 1, 20, 0));

        await _handler.Handle(new GetAdminCompanyReviewsQuery(), CancellationToken.None);

        capturedFilter.Should().NotBeNull();
        capturedFilter!.Status.Should().BeNull();
        capturedFilter.Sentiment.Should().BeNull();
        capturedFilter.SearchQuery.Should().BeNull();
        _reviewRepository.Verify(
            repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPagination_Normalizes()
    {
        _reviewRepository
            .Setup(repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<AdminCompanyReviewListItemDto>(new List<AdminCompanyReviewListItemDto>(), 1, 20, 0));

        await _handler.Handle(new GetAdminCompanyReviewsQuery { Page = -3, PageSize = 5000 }, CancellationToken.None);

        _reviewRepository.Verify(
            repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesSearchStatusAndSentimentIntoFilter()
    {
        AdminCompanyReviewFilter? capturedFilter = null;
        _reviewRepository
            .Setup(repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 2, 10, It.IsAny<CancellationToken>()))
            .Callback<AdminCompanyReviewFilter, int, int, CancellationToken>((filter, _, _, _) => capturedFilter = filter)
            .ReturnsAsync(new PaginatedList<AdminCompanyReviewListItemDto>(new List<AdminCompanyReviewListItemDto>(), 2, 10, 0));

        await _handler.Handle(
            new GetAdminCompanyReviewsQuery
            {
                SearchQuery = "acme",
                Status = CompanyReviewStatus.Published,
                Sentiment = ReviewSentiment.Negative,
                Page = 2,
                PageSize = 10
            },
            CancellationToken.None);

        capturedFilter!.SearchQuery.Should().Be("acme");
        capturedFilter.Status.Should().Be(CompanyReviewStatus.Published);
        capturedFilter.Sentiment.Should().Be(ReviewSentiment.Negative);
    }

    [Fact]
    public async Task Handle_ResolvesReviewerNameAndEmailForExistingUser()
    {
        var item = MakeItem("talent-1");
        _reviewRepository
            .Setup(repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<AdminCompanyReviewListItemDto>(new List<AdminCompanyReviewListItemDto> { item }, 1, 20, 1));
        _userRepository
            .Setup(repository => repository.GetByUserIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { new Talent("talent-1", "Jordan Smith", "jordan.smith@ait.com") });

        var result = await _handler.Handle(new GetAdminCompanyReviewsQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].ReviewerName.Should().Be("Jordan Smith");
        result.Items[0].ReviewerEmail.Should().Be("jordan.smith@ait.com");
        result.Items[0].ReviewerUserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenReviewerAccountIsDeleted_ShowsDeletedUserWithNullId()
    {
        var item = MakeItem("gone-talent");
        _reviewRepository
            .Setup(repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<AdminCompanyReviewListItemDto>(new List<AdminCompanyReviewListItemDto> { item }, 1, 20, 1));

        var result = await _handler.Handle(new GetAdminCompanyReviewsQuery(), CancellationToken.None);

        result.Items[0].ReviewerName.Should().Be("Deleted user");
        result.Items[0].ReviewerEmail.Should().BeEmpty();
        result.Items[0].ReviewerUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ReturnsEmptyPaginatedList()
    {
        _reviewRepository
            .Setup(repository => repository.GetAdminPagedAsync(It.IsAny<AdminCompanyReviewFilter>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<AdminCompanyReviewListItemDto>(new List<AdminCompanyReviewListItemDto>(), 1, 20, 0));

        var result = await _handler.Handle(new GetAdminCompanyReviewsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _userRepository.Verify(
            repository => repository.GetByUserIdsAsync(It.Is<IEnumerable<string>>(ids => !ids.Any()), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
