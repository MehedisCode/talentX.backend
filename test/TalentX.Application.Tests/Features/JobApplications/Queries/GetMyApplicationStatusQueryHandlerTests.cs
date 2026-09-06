using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobApplications.Queries.GetMyApplicationStatus;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobApplications.Queries;

public class GetMyApplicationStatusQueryHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IJobApplicationRepository> _jobApplicationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetMyApplicationStatusQueryHandler _handler;

    public GetMyApplicationStatusQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Jobs).Returns(_jobRepository.Object);
        _unitOfWork.Setup(u => u.JobApplications).Returns(_jobApplicationRepository.Object);
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");
        _handler = new GetMyApplicationStatusQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static Job PublishedJob()
    {
        var job = Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            new List<string> { "C#" },
            1,
            DateTime.UtcNow.AddDays(30));
        job.MarkAsPublished();
        return job;
    }

    [Fact]
    public async Task Handle_WhenTalentHasApplied_ReturnsHasAppliedTrue()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.ExistsByJobIdAndTalentUserIdAsync(job.Id, "talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new GetMyApplicationStatusQuery(job.Id), CancellationToken.None);

        result.HasApplied.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTalentHasNotApplied_ReturnsHasAppliedFalse()
    {
        var job = PublishedJob();
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.ExistsByJobIdAndTalentUserIdAsync(job.Id, "talent-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(new GetMyApplicationStatusQuery(job.Id), CancellationToken.None);

        result.HasApplied.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UsesTheCallerIdFromTheTokenAsTheTalentId()
    {
        var job = PublishedJob();
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-from-token");
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _jobApplicationRepository
            .Setup(r => r.ExistsByJobIdAndTalentUserIdAsync(job.Id, "talent-from-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(new GetMyApplicationStatusQuery(job.Id), CancellationToken.None);

        result.HasApplied.Should().BeTrue();
        _jobApplicationRepository.Verify(
            r => r.ExistsByJobIdAndTalentUserIdAsync(job.Id, "talent-from-token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenJobIsMissingOrNotPublished_ThrowsNotFoundException()
    {
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(new GetMyApplicationStatusQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenJobIsMissingOrNotPublished_DoesNotQueryApplications()
    {
        _jobRepository
            .Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        try
        {
            await _handler.Handle(new GetMyApplicationStatusQuery(Guid.NewGuid()), CancellationToken.None);
        }
        catch (NotFoundException)
        {
            // expected — assertion below verifies no application lookup happened.
        }

        _jobApplicationRepository.Verify(
            r => r.ExistsByJobIdAndTalentUserIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

}
