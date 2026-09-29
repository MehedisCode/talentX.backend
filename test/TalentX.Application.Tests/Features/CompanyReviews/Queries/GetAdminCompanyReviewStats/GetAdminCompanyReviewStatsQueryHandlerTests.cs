using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewStats;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetAdminCompanyReviewStats;

public sealed class GetAdminCompanyReviewStatsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly GetAdminCompanyReviewStatsQueryHandler _handler;

    public GetAdminCompanyReviewStatsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _handler = new GetAdminCompanyReviewStatsQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_MapsCountsPerStatusAndTotal()
    {
        _reviewRepository
            .Setup(repository => repository.GetStatusCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CompanyReviewStatus, int>
            {
                [CompanyReviewStatus.Pending] = 3,
                [CompanyReviewStatus.Published] = 5,
                [CompanyReviewStatus.Rejected] = 1,
                [CompanyReviewStatus.Hidden] = 2
            });

        var result = await _handler.Handle(new GetAdminCompanyReviewStatsQuery(), CancellationToken.None);

        result.PendingCount.Should().Be(3);
        result.PublishedCount.Should().Be(5);
        result.RejectedCount.Should().Be(1);
        result.HiddenCount.Should().Be(2);
        result.WithdrawnCount.Should().Be(0);
        result.Total.Should().Be(11);
    }

    [Fact]
    public async Task Handle_WithNoReviews_ReturnsAllZeros()
    {
        _reviewRepository
            .Setup(repository => repository.GetStatusCountsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CompanyReviewStatus, int>());

        var result = await _handler.Handle(new GetAdminCompanyReviewStatsQuery(), CancellationToken.None);

        result.Total.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.PublishedCount.Should().Be(0);
        result.RejectedCount.Should().Be(0);
        result.HiddenCount.Should().Be(0);
        result.WithdrawnCount.Should().Be(0);
    }
}
