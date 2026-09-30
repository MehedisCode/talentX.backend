using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Features.Employers.Queries.GetEmployerDashboard;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Employers.Queries;

public sealed class GetEmployerDashboardQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetEmployerDashboardQueryHandler _handler;

    public GetEmployerDashboardQueryHandlerTests()
    {
        _handler = new GetEmployerDashboardQueryHandler(
            _jobRepository.Object,
            _jobApplicationRepository.Object,
            _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var handleAction = () => _handler.Handle(new GetEmployerDashboardQuery(), CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to view employer dashboard.");
        _jobRepository.Verify(repository => repository.GetEmployerStatsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _jobApplicationRepository.Verify(repository => repository.GetCandidatePipelineStatsByEmployerIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerIsAuthenticated_ReturnsEmployerDashboardDto()
    {
        const string currentEmployerUserId = "employer-user-123";
        _currentUserService.SetupGet(service => service.UserId).Returns(currentEmployerUserId);

        var expectedJobStats = new EmployerJobStatsDto(
            TotalPostedJobs: 14,
            ActiveJobsCount: 6,
            DraftJobsCount: 3,
            ClosedJobsCount: 2,
            ExpiredJobsCount: 1,
            DeactivatedJobsCount: 1,
            PendingReviewJobsCount: 1);

        var expectedPipelineStats = new CandidatePipelineStatsDto(
            AppliedCount: 10,
            ShortlistedCount: 5,
            InvitedCount: 3,
            InterviewCount: 4,
            RejectedCount: 2,
            HiredCount: 4);

        var recentJobsList = new List<EmployerRecentJobDto>
        {
            new(
                Id: Guid.NewGuid(),
                Title: "Senior .NET Engineer",
                Status: "Published",
                CreatedAt: DateTime.UtcNow.AddDays(-2),
                ExpireAt: DateTime.UtcNow.AddDays(28),
                Vacancies: 2,
                ApplicantCount: 7)
        };

        _jobRepository.Setup(repository => repository.GetEmployerStatsAsync(currentEmployerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedJobStats);

        _jobApplicationRepository.Setup(repository => repository.GetCandidatePipelineStatsByEmployerIdAsync(currentEmployerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPipelineStats);

        _jobRepository.Setup(repository => repository.GetRecentJobsByEmployerIdAsync(
                currentEmployerUserId,
                EmployerDashboardConstants.RecentJobsLimit,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentJobsList);

        var dashboardResult = await _handler.Handle(new GetEmployerDashboardQuery(), CancellationToken.None);

        dashboardResult.Should().NotBeNull();
        dashboardResult.TotalPostedJobs.Should().Be(14);
        dashboardResult.TotalHires.Should().Be(4);
        dashboardResult.ActiveJobsCount.Should().Be(6);
        dashboardResult.DraftJobsCount.Should().Be(3);
        dashboardResult.ClosedJobsCount.Should().Be(2);
        dashboardResult.ExpiredJobsCount.Should().Be(1);
        dashboardResult.DeactivatedJobsCount.Should().Be(1);
        dashboardResult.PendingReviewJobsCount.Should().Be(1);
        dashboardResult.CandidatePipeline.Should().Be(expectedPipelineStats);
        dashboardResult.RecentJobs.Should().HaveCount(1);
        dashboardResult.RecentJobs[0].Title.Should().Be("Senior .NET Engineer");
        dashboardResult.RecentJobs[0].ApplicantCount.Should().Be(7);
    }
}
