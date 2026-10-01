using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Features.CompanyReviews.Queries.GetPublicCompanyReviews;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetPublicCompanyReviews;

public sealed class GetPublicCompanyReviewsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICompanyReviewRepository> _reviewRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly GetPublicCompanyReviewsQueryHandler _handler;

    public GetPublicCompanyReviewsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.CompanyReviews).Returns(_reviewRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Companies).Returns(_companyRepository.Object);
        _handler = new GetPublicCompanyReviewsQueryHandler(_unitOfWork.Object);
    }

    private static CompanyReview MakePublished(
        int? recruitment = 4, int? communication = 4, int? interview = 4, int? professionalism = 4)
    {
        var review = CompanyReview.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "talent-1", "Backend Engineer",
            ReviewSentiment.Positive, "Great process", recruitment, communication, interview, professionalism);
        review.Approve("admin-1");
        return review;
    }

    [Fact]
    public async Task Handle_CompanyFoundById_ReturnsMappedPublishedReviews()
    {
        var companyId = Guid.NewGuid();
        var company = new Company("employer-1", "Acme Inc");
        _companyRepository.Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        var review = MakePublished();
        _reviewRepository
            .Setup(repository => repository.GetPublishedByCompanyIdPagedAsync(company.Id, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(new List<CompanyReview> { review }, 1, 20, 1));

        var result = await _handler.Handle(new GetPublicCompanyReviewsQuery(companyId), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(review.Id);
        result.Items[0].Sentiment.Should().Be(review.Sentiment);
        result.Items[0].Comment.Should().Be(review.Comment);
        result.Items[0].RecruitmentProcessRating.Should().Be(review.RecruitmentProcessRating);
        result.Items[0].PublishedAt.Should().Be(review.PublishedAt!.Value);
    }

    [Fact]
    public async Task Handle_CompanyNotFoundById_FallsBackToEmployerIdLookup()
    {
        var companyId = Guid.NewGuid();
        var company = new Company(companyId.ToString(), "Acme Inc");
        _companyRepository.Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(companyId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetPublishedByCompanyIdPagedAsync(company.Id, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(new List<CompanyReview>(), 1, 20, 0));

        var result = await _handler.Handle(new GetPublicCompanyReviewsQuery(companyId), CancellationToken.None);

        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CompanyNotFoundEitherWay_ThrowsNotFoundException()
    {
        var companyId = Guid.NewGuid();
        _companyRepository.Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);
        _companyRepository.Setup(repository => repository.GetByEmployerIdAsync(companyId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetPublicCompanyReviewsQuery(companyId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NormalizesPagingBeforeQueryingRepository()
    {
        var companyId = Guid.NewGuid();
        var company = new Company("employer-1", "Acme Inc");
        _companyRepository.Setup(repository => repository.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(company);
        _reviewRepository
            .Setup(repository => repository.GetPublishedByCompanyIdPagedAsync(company.Id, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<CompanyReview>(new List<CompanyReview>(), 1, 20, 0));

        await _handler.Handle(new GetPublicCompanyReviewsQuery(companyId, Page: -5, PageSize: 5000), CancellationToken.None);

        _reviewRepository.Verify(
            repository => repository.GetPublishedByCompanyIdPagedAsync(company.Id, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void PublicCompanyReviewDto_Shape_IsLockedToTheMinimalAnonymizedSet()
    {
        var propertyNames = typeof(PublicCompanyReviewDto).GetProperties().Select(property => property.Name);

        propertyNames.Should().BeEquivalentTo(new[]
        {
            nameof(PublicCompanyReviewDto.Id),
            nameof(PublicCompanyReviewDto.Sentiment),
            nameof(PublicCompanyReviewDto.Comment),
            nameof(PublicCompanyReviewDto.RecruitmentProcessRating),
            nameof(PublicCompanyReviewDto.CommunicationRating),
            nameof(PublicCompanyReviewDto.InterviewExperienceRating),
            nameof(PublicCompanyReviewDto.ProfessionalismRating),
            nameof(PublicCompanyReviewDto.PublishedAt)
        });
    }
}
