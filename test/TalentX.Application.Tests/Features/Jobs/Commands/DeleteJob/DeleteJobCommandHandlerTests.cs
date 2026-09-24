using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.Commands.DeleteJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.DeleteJob;

public sealed class DeleteJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly DeleteJobCommandHandler _handler;

    public DeleteJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("employer-123");
        _handler = new DeleteJobCommandHandler(
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

        var command = new DeleteJobCommand(Guid.NewGuid());
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to delete a job.");
        _jobRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        var targetJobId = Guid.NewGuid();
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = new DeleteJobCommand(targetJobId);
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

        var command = new DeleteJobCommand(targetJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to delete this job.");
        _jobRepository.Verify(repository => repository.Update(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobBelongsToCurrentEmployer_SoftDeletesJobAndSavesChanges()
    {
        var targetJob = CreateSampleJob(employerId: "employer-123");
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetJob);

        var command = new DeleteJobCommand(targetJob.Id);
        var commandResult = await _handler.Handle(command, CancellationToken.None);

        commandResult.Should().Be(MediatR.Unit.Value);
        targetJob.IsDeleted.Should().BeTrue();
        targetJob.DeletedAt.Should().NotBeNull();
        targetJob.UpdatedAt.Should().NotBeNull();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
