using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.SavedJobs.Commands.SaveJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.SavedJobs.Commands.SaveJob;

public class SaveJobCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ISavedJobRepository> _savedJobRepository = new();
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly SaveJobCommandHandler _handler;

    public SaveJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("talent-1");
        _unitOfWork.Setup(u => u.SavedJobs).Returns(_savedJobRepository.Object);
        _unitOfWork.Setup(u => u.Jobs).Returns(_jobRepository.Object);
        _handler = new SaveJobCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    private static Job CreatePublishedJob(Guid? jobId = null)
    {
        return Job.Create(
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
    }

    [Fact]
    public async Task Handle_SavesJobSuccessfully()
    {
        var job = CreatePublishedJob();
        _jobRepository.Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _savedJobRepository.Setup(r => r.FindAsync("talent-1", job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SavedJob?)null);

        var result = await _handler.Handle(new SaveJobCommand(job.Id), CancellationToken.None);

        result.Should().NotBeEmpty();
        _savedJobRepository.Verify(r => r.AddAsync(It.IsAny<SavedJob>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
    {
        _jobRepository.Setup(r => r.GetPublishedJobByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(new SaveJobCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadySaved_ThrowsConflictException()
    {
        var job = CreatePublishedJob();
        var existingSavedJob = SavedJob.Create(job.Id, "talent-1");
        _jobRepository.Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _savedJobRepository.Setup(r => r.FindAsync("talent-1", job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSavedJob);

        var act = () => _handler.Handle(new SaveJobCommand(job.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(new SaveJobCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenJobIsClosed_ThrowsNotFoundException()
    {
        var job = CreatePublishedJob();
        job.MarkAsClosed();
        _jobRepository.Setup(r => r.GetPublishedJobByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(new SaveJobCommand(job.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
