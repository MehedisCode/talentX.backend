using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;

public sealed class GetEmployerCompanyReviewsQueryHandlerTests
{
    private const string EmployerId = "employer-1";

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetEmployerCompanyReviewsQueryHandler _handler;

    public GetEmployerCompanyReviewsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Companies).Returns(_companyRepository.Object);
        _currentUserService.SetupGet(service => service.UserId).Returns(EmployerId);
        _handler = new GetEmployerCompanyReviewsQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static CompanyReview MakeReview(CompanyReviewStatus status)
    {
        var review = CompanyReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "talent-1", "Backend Engineer",
            ReviewSentiment.Positive, "Great process", 4, 4, 4, 4);

        switch (status)
        {
            case CompanyReviewStatus.Published:
                review.Approve("admin-1");
                break;
            case CompanyReviewStatus.Rejected:
                review.Reject(ReviewModerationReason.Spam, "spam", "admin-1");
                break;
            case CompanyReviewStatus.Hidden:
                review.Approve("admin-1");
                review.Hide(ReviewModerationReason.Other, "policy", "admin-1");
                break;
            case CompanyReviewStatus.Withdrawn:
                review.Withdraw();
                break;
        }

        return review;
    }

    [Fact]
    public async Task Handle_NoCompanyForEmployer_ReturnsEmptyPaginatedListWithoutQueryingReviews()
    {
        _companyRepository
            .Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var result = await _handler.Handle(new GetEmployerCompanyReviewsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _reviewRepository.Verify(
            repository => repository.GetByCompanyIdPagedAsync(
                It.IsAny<Guid>(), It.IsAny<CompanyReviewStatus?>(), It.IsAny<ReviewSentiment?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UsesOnlyTheCallingEmployersOwnCompany()
    {
        var company = new Company(EmployerId, "Acme Inc");
        _companyRepository
            .Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetByCompanyIdPagedAsync(company.Id, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(new List<CompanyReview>(), 1, 20, 0));

        await _handler.Handle(new GetEmployerCompanyReviewsQuery(), CancellationToken.None);

        _reviewRepository.Verify(
            repository => repository.GetByCompanyIdPagedAsync(company.Id, null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentEmployers_ResolveOnlyTheirOwnCompany()
    {
        var companyOne = new Company("employer-1", "Acme Inc");
        var companyTwo = new Company("employer-2", "Globex Corp");
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync("employer-1", It.IsAny<CancellationToken>())).ReturnsAsync(companyOne);
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync("employer-2", It.IsAny<CancellationToken>())).ReturnsAsync(companyTwo);
        _reviewRepository
            .Setup(repository => repository.GetByCompanyIdPagedAsync(It.IsAny<Guid>(), null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(new List<CompanyReview>(), 1, 20, 0));
        _currentUserService.SetupGet(service => service.UserId).Returns("employer-2");

        await _handler.Handle(new GetEmployerCompanyReviewsQuery(), CancellationToken.None);

        _reviewRepository.Verify(
            repository => repository.GetByCompanyIdPagedAsync(companyTwo.Id, null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
        _reviewRepository.Verify(
            repository => repository.GetByCompanyIdPagedAsync(companyOne.Id, It.IsAny<CompanyReviewStatus?>(), It.IsAny<ReviewSentiment?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MapsEveryStatus_IncludingWithdrawnHiddenAndRejected()
    {
        var company = new Company(EmployerId, "Acme Inc");
        var reviews = new List<CompanyReview>
        {
            MakeReview(CompanyReviewStatus.Pending),
            MakeReview(CompanyReviewStatus.Published),
            MakeReview(CompanyReviewStatus.Rejected),
            MakeReview(CompanyReviewStatus.Hidden),
            MakeReview(CompanyReviewStatus.Withdrawn)
        };
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetByCompanyIdPagedAsync(company.Id, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(reviews, 1, 20, reviews.Count));

        var result = await _handler.Handle(new GetEmployerCompanyReviewsQuery(), CancellationToken.None);

        result.Items.Select(item => item.Status).Should().BeEquivalentTo(new[]
        {
            CompanyReviewStatus.Pending,
            CompanyReviewStatus.Published,
            CompanyReviewStatus.Rejected,
            CompanyReviewStatus.Hidden,
            CompanyReviewStatus.Withdrawn
        });
    }

    [Fact]
    public async Task Handle_NeverExposesModerationOrReviewerFields()
    {
        var company = new Company(EmployerId, "Acme Inc");
        var review = MakeReview(CompanyReviewStatus.Hidden);
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetByCompanyIdPagedAsync(company.Id, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(new List<CompanyReview> { review }, 1, 20, 1));

        var result = await _handler.Handle(new GetEmployerCompanyReviewsQuery(), CancellationToken.None);

        var dto = result.Items.Single();
        dto.Id.Should().Be(review.Id);
        dto.JobTitle.Should().Be(review.JobTitle);
        typeof(EmployerCompanyReviewDto).GetProperty("TalentUserId").Should().BeNull();
        typeof(EmployerCompanyReviewDto).GetProperty("ModerationReason").Should().BeNull();
        typeof(EmployerCompanyReviewDto).GetProperty("ModerationDetails").Should().BeNull();
    }

    [Fact]
    public async Task Handle_PassesFiltersAndNormalizedPagingToRepository()
    {
        var company = new Company(EmployerId, "Acme Inc");
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(EmployerId, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetByCompanyIdPagedAsync(company.Id, CompanyReviewStatus.Published, ReviewSentiment.Negative, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(new List<CompanyReview>(), 1, 20, 0));

        await _handler.Handle(
            new GetEmployerCompanyReviewsQuery { Status = CompanyReviewStatus.Published, Sentiment = ReviewSentiment.Negative, Page = -5, PageSize = 5000 },
            CancellationToken.None);

        _reviewRepository.Verify(
            repository => repository.GetByCompanyIdPagedAsync(company.Id, CompanyReviewStatus.Published, ReviewSentiment.Negative, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void EmployerCompanyReviewDto_Shape_IsLockedToTheMinimalAnonymizedSet()
    {
        var propertyNames = typeof(EmployerCompanyReviewDto).GetProperties().Select(property => property.Name);

        propertyNames.Should().BeEquivalentTo(new[]
        {
            nameof(EmployerCompanyReviewDto.Id),
            nameof(EmployerCompanyReviewDto.Status),
            nameof(EmployerCompanyReviewDto.Sentiment),
            nameof(EmployerCompanyReviewDto.Comment),
            nameof(EmployerCompanyReviewDto.Ratings),
            nameof(EmployerCompanyReviewDto.JobTitle),
            nameof(EmployerCompanyReviewDto.SubmittedAt),
            nameof(EmployerCompanyReviewDto.UpdatedAt)
        });
    }
}
