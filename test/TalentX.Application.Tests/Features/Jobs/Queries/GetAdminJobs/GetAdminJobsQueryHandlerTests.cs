using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetAdminJobs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Queries.GetAdminJobs;

public sealed class GetAdminJobsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly GetAdminJobsQueryHandler _handler;

    public GetAdminJobsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Jobs).Returns(_jobRepository.Object);
        _handler = new GetAdminJobsQueryHandler(_unitOfWork.Object);
    }

    private void SetupRepository(AdminJobQueryParams expectedParams, PaginatedList<AdminJobListItemDto>? result = null)
    {
        _jobRepository
            .Setup(repository => repository.GetAdminJobsPagedAsync(expectedParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? new PaginatedList<AdminJobListItemDto>(
                new List<AdminJobListItemDto>(), expectedParams.Page, expectedParams.PageSize, 0));
    }

    [Fact]
    public async Task Handle_WithDefaultQuery_UsesDefaultPaginationAndReturnsRepositoryResult()
    {
        var expectedParams = new AdminJobQueryParams { Page = 1, PageSize = 20 };
        var repositoryResult = new PaginatedList<AdminJobListItemDto>(
            new List<AdminJobListItemDto>
            {
                new() { Title = "Backend Engineer", EmployerEmail = "employer@talentx.com", Status = JobStatus.PendingReview }
            },
            1,
            20,
            1);
        SetupRepository(expectedParams, repositoryResult);

        var result = await _handler.Handle(new GetAdminJobsQuery(), CancellationToken.None);

        result.Should().BeSameAs(repositoryResult);
        result.Items.Should().ContainSingle(item => item.Title == "Backend Engineer");
        _jobRepository.Verify(
            repository => repository.GetAdminJobsPagedAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchAndStatus_PassesEveryParameterToRepository()
    {
        var expectedParams = new AdminJobQueryParams
        {
            SearchQuery = "acme",
            Status = JobStatus.Deactivated,
            Page = 2,
            PageSize = 10
        };
        SetupRepository(expectedParams);

        await _handler.Handle(
            new GetAdminJobsQuery { SearchQuery = "acme", Status = JobStatus.Deactivated, Page = 2, PageSize = 10 },
            CancellationToken.None);

        _jobRepository.Verify(
            repository => repository.GetAdminJobsPagedAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidPagination_NormalizesBeforeQuerying()
    {
        var expectedParams = new AdminJobQueryParams { Page = 1, PageSize = 20 };
        SetupRepository(expectedParams);

        await _handler.Handle(new GetAdminJobsQuery { Page = -3, PageSize = 5000 }, CancellationToken.None);

        _jobRepository.Verify(
            repository => repository.GetAdminJobsPagedAsync(expectedParams, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
