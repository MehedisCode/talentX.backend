using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.SavedJobs.Queries.GetSavedJobs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.SavedJobs.Queries.GetSavedJobs;

public class GetSavedJobsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ISavedJobRepository> _savedJobRepository = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly GetSavedJobsQueryHandler _handler;

    public GetSavedJobsQueryHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");
        _unitOfWork.Setup(u => u.SavedJobs).Returns(_savedJobRepository.Object);
        _unitOfWork.Setup(u => u.Jobs).Returns(_jobRepository.Object);
        _handler = new GetSavedJobsQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static Job CreatePublishedJob(Guid? jobId = null)
    {
        var id = jobId ?? Guid.NewGuid();
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Hybrid,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }

    [Fact]
    public async Task Handle_ReturnsSavedJobsForTalent()
    {
        var job = CreatePublishedJob();
        var savedJob = SavedJob.Create(job.Id, "talent-1");

        var paginatedResult = new PaginatedList<SavedJob>(
            new List<SavedJob> { savedJob }, 1, 20, 1);

        _savedJobRepository.Setup(r => r.GetSavedJobsAsync("talent-1", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        _jobRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new GetSavedJobsQuery(1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().JobTitle.Should().Be("Backend Engineer");
        result.Items.First().CompanyName.Should().Be("Acme Corp");
        result.Items.First().PostedDate.Should().Be(job.CreatedAt);
    }

    [Fact]
    public async Task Handle_WhenNoSavedJobs_ReturnsEmptyList()
    {
        var paginatedResult = new PaginatedList<SavedJob>(
            new List<SavedJob>(), 1, 20, 0);

        _savedJobRepository.Setup(r => r.GetSavedJobsAsync("talent-1", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        _jobRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());

        var result = await _handler.Handle(new GetSavedJobsQuery(1, 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new GetSavedJobsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenJobDeleted_ShowsJobInfoWithIsAvailableFalse()
    {
        var job = CreatePublishedJob();
        job.SoftDelete();
        var savedJob = SavedJob.Create(job.Id, "talent-1");

        var paginatedResult = new PaginatedList<SavedJob>(
            new List<SavedJob> { savedJob }, 1, 20, 1);

        _savedJobRepository.Setup(r => r.GetSavedJobsAsync("talent-1", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        _jobRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new GetSavedJobsQuery(1, 20), CancellationToken.None);

        result.Items.First().JobTitle.Should().Be("Backend Engineer");
        result.Items.First().IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenJobExpired_ShowsIsAvailableFalse()
    {
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Hybrid,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(-1));
        job.MarkAsPublished();

        var savedJob = SavedJob.Create(job.Id, "talent-1");

        var paginatedResult = new PaginatedList<SavedJob>(
            new List<SavedJob> { savedJob }, 1, 20, 1);

        _savedJobRepository.Setup(r => r.GetSavedJobsAsync("talent-1", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        _jobRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new GetSavedJobsQuery(1, 20), CancellationToken.None);

        result.Items.First().IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PostedDateMapsFromJobCreatedAt()
    {
        var job = CreatePublishedJob();
        var savedJob = SavedJob.Create(job.Id, "talent-1");

        var paginatedResult = new PaginatedList<SavedJob>(
            new List<SavedJob> { savedJob }, 1, 20, 1);

        _savedJobRepository.Setup(r => r.GetSavedJobsAsync("talent-1", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        _jobRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { job });

        var result = await _handler.Handle(new GetSavedJobsQuery(1, 20), CancellationToken.None);

        result.Items.First().PostedDate.Should().Be(job.CreatedAt);
        result.Items.First().PostedDate.Should().NotBe(savedJob.CreatedAt);
    }

    [Fact]
    public async Task Handle_WithMixedAvailability_ReturnsCorrectStates()
    {
        var availableJob = CreatePublishedJob();
        var deletedJob = CreatePublishedJob();
        deletedJob.SoftDelete();

        var savedJobs = new List<SavedJob>
        {
            SavedJob.Create(availableJob.Id, "talent-1"),
            SavedJob.Create(deletedJob.Id, "talent-1")
        };

        var paginatedResult = new PaginatedList<SavedJob>(savedJobs, 1, 20, 2);

        _savedJobRepository.Setup(r => r.GetSavedJobsAsync("talent-1", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        _jobRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job> { availableJob, deletedJob });

        var result = await _handler.Handle(new GetSavedJobsQuery(1, 20), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items.First(i => i.JobId == availableJob.Id).IsAvailable.Should().BeTrue();
        result.Items.First(i => i.JobId == deletedJob.Id).IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PaginationPassesCorrectParameters()
    {
        var paginatedResult = new PaginatedList<SavedJob>(
            new List<SavedJob>(), 2, 10, 0);

        _savedJobRepository.Setup(r => r.GetSavedJobsAsync("talent-1", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);
        _jobRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Job>());

        await _handler.Handle(new GetSavedJobsQuery(2, 10), CancellationToken.None);

        _savedJobRepository.Verify(r => r.GetSavedJobsAsync("talent-1", 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
