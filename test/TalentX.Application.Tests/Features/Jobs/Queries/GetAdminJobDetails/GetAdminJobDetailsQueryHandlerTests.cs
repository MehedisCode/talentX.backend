using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Jobs.Queries.GetAdminJobDetails;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Queries.GetAdminJobDetails;

public sealed class GetAdminJobDetailsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJobModerationHistoryRepository> _historyRepository = new();
    private readonly GetAdminJobDetailsQueryHandler _handler;

    public GetAdminJobDetailsQueryHandlerTests()
    {
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.Users).Returns(_userRepository.Object);
        _unitOfWork.SetupGet(unitOfWork => unitOfWork.JobModerationHistories).Returns(_historyRepository.Object);
        _historyRepository
            .Setup(repository => repository.GetByJobIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobModerationHistory>());
        _handler = new GetAdminJobDetailsQueryHandler(_unitOfWork.Object);
    }

    private static Job CreateJob(string employerId = "employer-1")
    {
        return Job.Create(
            employerId,
            "Senior Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            ["C#", "PostgreSQL"],
            2,
            DateTime.UtcNow.AddDays(30));
    }

    private void SetupJob(Job job)
    {
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
    }

    private void SetupEmployer(string userId, string name = "Ayesha Rahman", string email = "employer@talentx.com")
    {
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new User(userId, name, email, UserRole.Employer, isVerified: true));
    }

    [Fact]
    public async Task Handle_WhenJobIsDeactivated_ReturnsDetailsWithReasonLabelAndEmployer()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        job.Deactivate(DeactivationReason.MisleadingInformation, "Salary does not match the description.");
        SetupJob(job);
        SetupEmployer("employer-1");

        var result = await _handler.Handle(new GetAdminJobDetailsQuery(job.Id), CancellationToken.None);

        result.Id.Should().Be(job.Id);
        result.Title.Should().Be("Senior Backend Engineer");
        result.Status.Should().Be(JobStatus.Deactivated);
        result.IsExpired.Should().BeFalse();
        result.EmployerName.Should().Be("Ayesha Rahman");
        result.EmployerEmail.Should().Be("employer@talentx.com");
        result.DeactivationReason.Should().Be(DeactivationReason.MisleadingInformation);
        result.DeactivationReasonLabel.Should().Be("Misleading or invalid information");
        result.DeactivationDetails.Should().Be("Salary does not match the description.");
        result.DeactivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReturnsBothTheFirstPostedAndTheLastPublishedDates()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        var firstPublishedAt = job.FirstPublishedAt;
        Thread.Sleep(5);
        job.Deactivate(DeactivationReason.Spam, "Posting spam links to another site.");
        job.Reactivate();
        SetupJob(job);
        SetupEmployer("employer-1");

        var result = await _handler.Handle(new GetAdminJobDetailsQuery(job.Id), CancellationToken.None);

        result.FirstPublishedAt.Should().Be(firstPublishedAt);
        result.PublishedAt.Should().Be(job.PublishedAt);
        result.PublishedAt.Should().BeAfter(firstPublishedAt!.Value);
    }

    [Fact]
    public async Task Handle_MapsModerationHistoryNewestFirstWithLabels()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        SetupJob(job);
        SetupEmployer("employer-1");

        var deactivated = JobModerationHistory.Create(
            job.Id, JobModerationAction.Deactivated, JobStatus.Published, JobStatus.Deactivated,
            "admin-1", "admin@talentx.com", DeactivationReason.Spam, "Posting spam links.");
        var reactivated = JobModerationHistory.Create(
            job.Id, JobModerationAction.Reactivated, JobStatus.Deactivated, JobStatus.Published,
            "admin-1", "admin@talentx.com");

        _historyRepository
            .Setup(repository => repository.GetByJobIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<JobModerationHistory> { reactivated, deactivated });

        var result = await _handler.Handle(new GetAdminJobDetailsQuery(job.Id), CancellationToken.None);

        result.History.Should().HaveCount(2);
        result.History[0].Action.Should().Be(JobModerationAction.Reactivated);
        result.History[0].ReasonLabel.Should().BeNull();
        result.History[1].Action.Should().Be(JobModerationAction.Deactivated);
        result.History[1].ReasonLabel.Should().Be("Spam");
        result.History[1].Details.Should().Be("Posting spam links.");
        result.History[1].ActorEmail.Should().Be("admin@talentx.com");
    }

    [Fact]
    public async Task Handle_WhenEmployerRecordIsMissing_ReturnsEmptyEmployerFields()
    {
        var job = CreateJob("ghost-employer");
        job.MarkAsPublished();
        SetupJob(job);
        _userRepository
            .Setup(repository => repository.GetByUserIdAsync("ghost-employer"))
            .ReturnsAsync((User?)null);

        var result = await _handler.Handle(new GetAdminJobDetailsQuery(job.Id), CancellationToken.None);

        result.EmployerName.Should().BeEmpty();
        result.EmployerEmail.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenJobIsDraft_ThrowsNotFoundException()
    {
        var job = CreateJob();
        SetupJob(job);

        var handleAction = () => _handler.Handle(new GetAdminJobDetailsQuery(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
        _historyRepository.Verify(
            repository => repository.GetByJobIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobIsClosed_ThrowsNotFoundException()
    {
        var job = CreateJob();
        job.MarkAsPublished();
        job.MarkAsClosed();
        SetupJob(job);

        var handleAction = () => _handler.Handle(new GetAdminJobDetailsQuery(job.Id), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var missingJobId = Guid.NewGuid();
        _jobRepository
            .Setup(repository => repository.GetByIdAsync(missingJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var handleAction = () => _handler.Handle(new GetAdminJobDetailsQuery(missingJobId), CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>();
    }
}
