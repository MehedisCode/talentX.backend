using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Features.Employers.Queries.GetEmployerJobs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers.Queries;

public sealed class GetEmployerJobsQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetEmployerJobsQueryHandler _handler;

    public GetEmployerJobsQueryHandlerTests()
    {
        _handler = new GetEmployerJobsQueryHandler(
            _jobRepository.Object,
            _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var query = new GetEmployerJobsQuery();
        var handleAction = () => _handler.Handle(query, CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to view your jobs.");
        _jobRepository.Verify(
            repository => repository.GetEmployerJobsPaginatedAsync(
                It.IsAny<string>(),
                It.IsAny<JobStatus?>(),
                It.IsAny<bool?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerIsAuthenticated_DispatchesWithNormalizedPagination()
    {
        const string currentEmployerUserId = "employer-user-456";
        _currentUserService.SetupGet(service => service.UserId).Returns(currentEmployerUserId);

        var jobListItems = new List<EmployerJobListItemDto>
        {
            new(
                Id: Guid.NewGuid(),
                Title: "Lead AI Engineer",
                CompanyName: "TechNova Corp",
                Status: "Published",
                EmploymentType: EmploymentType.FullTime,
                WorkArrangement: WorkArrangement.FullRemote,
                ExperienceLevel: ExperienceLevel.SeniorLevel,
                Location: "Dhaka",
                Vacancies: 1,
                ExpireAt: DateTime.UtcNow.AddDays(15),
                CreatedAt: DateTime.UtcNow.AddDays(-5),
                ApplicantCount: 12)
        };

        var expectedPaginatedList = new PaginatedList<EmployerJobListItemDto>(jobListItems, 1, 20, 1);

        _jobRepository.Setup(repository => repository.GetEmployerJobsPaginatedAsync(
                currentEmployerUserId,
                JobStatus.Published,
                false,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPaginatedList);

        var query = new GetEmployerJobsQuery(
            Page: -1,
            PageSize: -5,
            Status: JobStatus.Published,
            IsExpired: false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Title.Should().Be("Lead AI Engineer");
        _jobRepository.Verify(
            repository => repository.GetEmployerJobsPaginatedAsync(
                currentEmployerUserId,
                JobStatus.Published,
                false,
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
