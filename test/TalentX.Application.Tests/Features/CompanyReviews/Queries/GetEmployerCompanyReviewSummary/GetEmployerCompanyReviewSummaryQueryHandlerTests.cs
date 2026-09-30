using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.CompanyReviews;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviewSummary;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetEmployerCompanyReviewSummary;

public sealed class GetEmployerCompanyReviewSummaryQueryHandlerTests
{
    private const string EmployerId = "employer-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetEmployerCompanyReviewSummaryQueryHandler _handler;

    public GetEmployerCompanyReviewSummaryQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Companies).Returns(_companyRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns(EmployerId);
        _handler = new GetEmployerCompanyReviewSummaryQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_NoCompanyForEmployer_ReturnsEmptySummaryAndZeroCounts()
    {
        _companyRepository
            .Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var result = await _handler.Handle(new GetEmployerCompanyReviewSummaryQuery(), CancellationToken.None);

        result.Summary.Should().Be(CompanyReviewSummaryCalculator.Empty);
        result.CountsByStatus.Should().HaveCount(5);
        result.CountsByStatus.Values.Should().OnlyContain(count => count == 0);
        _reviewRepository.Verify(
            repository => repository.GetPublishedSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UsesOnlyTheCallingEmployersOwnCompany()
    {
        var company = new Company(EmployerId, "Acme Inc");
        var summary = new CompanyReviewSummaryDto(
            1, 1, 0, 100m,
            new RatingAreaSummaryDto(5m, 1),
            new RatingAreaSummaryDto(5m, 1),
            new RatingAreaSummaryDto(5m, 1),
            new RatingAreaSummaryDto(5m, 1));
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetPublishedSummaryAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        _reviewRepository
            .Setup(repository => repository.GetStatusCountsByCompanyAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CompanyReviewStatus, int> { [CompanyReviewStatus.Published] = 1 });

        var result = await _handler.Handle(new GetEmployerCompanyReviewSummaryQuery(), CancellationToken.None);

        result.Summary.Should().Be(summary);
        result.CountsByStatus[nameof(CompanyReviewStatus.Published)].Should().Be(1);
    }

    [Fact]
    public async Task Handle_FillsAllFiveStatusesEvenWhenSomeAreMissingFromTheRepositoryResult()
    {
        var company = new Company(EmployerId, "Acme Inc");
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetPublishedSummaryAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompanyReviewSummaryCalculator.Empty);
        _reviewRepository
            .Setup(repository => repository.GetStatusCountsByCompanyAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CompanyReviewStatus, int> { [CompanyReviewStatus.Pending] = 2 });

        var result = await _handler.Handle(new GetEmployerCompanyReviewSummaryQuery(), CancellationToken.None);

        result.CountsByStatus.Keys.Should().BeEquivalentTo(new[] { "Pending", "Published", "Rejected", "Hidden", "Withdrawn" });
        result.CountsByStatus[nameof(CompanyReviewStatus.Pending)].Should().Be(2);
        result.CountsByStatus[nameof(CompanyReviewStatus.Published)].Should().Be(0);
        result.CountsByStatus[nameof(CompanyReviewStatus.Rejected)].Should().Be(0);
        result.CountsByStatus[nameof(CompanyReviewStatus.Hidden)].Should().Be(0);
        result.CountsByStatus[nameof(CompanyReviewStatus.Withdrawn)].Should().Be(0);
    }

    [Fact]
    public async Task Handle_SummaryReflectsPublishedReviewsOnly()
    {
        var company = new Company(EmployerId, "Acme Inc");
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetStatusCountsByCompanyAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<CompanyReviewStatus, int>());
        _reviewRepository
            .Setup(repository => repository.GetPublishedSummaryAsync(company.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CompanyReviewSummaryCalculator.Empty);

        await _handler.Handle(new GetEmployerCompanyReviewSummaryQuery(), CancellationToken.None);

        _reviewRepository.Verify(repository => repository.GetPublishedSummaryAsync(company.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
