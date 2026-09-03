using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.Commands.CloseJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.CloseJob;

public sealed class CloseJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly CloseJobCommandHandler _handler;

    public CloseJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("employer-123");
        _handler = new CloseJobCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _currentUserService.Object);
    }

    private static Job CreateSampleJob(string employerId = "employer-123")
    {
        return Job.Create(
            employerId,
            "Senior Backend Engineer",
            "Tech Innovators Inc",
            "Develop robust distributed systems.",
            EmploymentType.FullTime,
            ExperienceLevel.SeniorLevel,
            "Dhaka",
            WorkArrangement.FullRemote,
            ["C#", ".NET", "PostgreSQL"],
            2,
            DateTime.UtcNow.AddDays(30));
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new CloseJobCommand(Guid.NewGuid());
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to close a job.");
        _jobRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobNotFound_ThrowsNotFoundException()
    {
        var targetJobId = Guid.NewGuid();
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = new CloseJobCommand(targetJobId);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job with ID '{targetJobId}' was not found.");
        _jobRepository.Verify(repository => repository.Update(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobBelongsToDifferentEmployer_ThrowsForbiddenException()
    {
        var targetJob = CreateSampleJob(employerId: "other-employer-999");
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetJob);

        var command = new CloseJobCommand(targetJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to close this job.");
        _jobRepository.Verify(repository => repository.Update(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobBelongsToCurrentEmployer_MarksJobAsClosedAndSavesChanges()
    {
        var targetJob = CreateSampleJob(employerId: "employer-123");
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetJob);

        var command = new CloseJobCommand(targetJob.Id);
        var commandResult = await _handler.Handle(command, CancellationToken.None);

        commandResult.Should().Be(MediatR.Unit.Value);
        targetJob.Status.Should().Be(JobStatus.Closed);
        targetJob.UpdatedAt.Should().NotBeNull();
        _jobRepository.Verify(repository => repository.Update(targetJob), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
