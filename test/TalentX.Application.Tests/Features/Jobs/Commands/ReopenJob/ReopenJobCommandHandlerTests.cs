using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.Commands.ReopenJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.ReopenJob;

public sealed class ReopenJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IJobAlertQueue> _jobAlertQueue = new();
    private readonly Mock<ILogger<ReopenJobCommandHandler>> _logger = new();
    private readonly ReopenJobCommandHandler _handler;

    public ReopenJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("employer-123");
        _handler = new ReopenJobCommandHandler(
            _jobRepository.Object,
            _unitOfWork.Object,
            _currentUserService.Object,
            _jobAlertQueue.Object,
            _logger.Object);
    }

    private static Job CreateSampleJob(string employerId = "employer-123", DateTime? expireAt = null)
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
            expireAt ?? DateTime.UtcNow.AddDays(30));
    }

    [Fact]
    public async Task Handle_ValidRequest_ReopensJobAndSaves()
    {
        var sampleJob = CreateSampleJob(employerId: "employer-123");
        sampleJob.MarkAsPublished();
        sampleJob.MarkAsClosed();
        _jobRepository.Setup(repository => repository.GetByIdAsync(sampleJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleJob);

        var command = new ReopenJobCommand(sampleJob.Id);
        var commandResult = await _handler.Handle(command, CancellationToken.None);

        commandResult.Should().Be(Unit.Value);
        sampleJob.Status.Should().Be(JobStatus.Published);
        sampleJob.UpdatedAt.Should().NotBeNull();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _jobAlertQueue.Verify(queue => queue.EnqueuePublishedJobAsync(sampleJob.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new ReopenJobCommand(Guid.NewGuid());
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("You must be signed in to reopen a job.");
        _jobRepository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_JobNotFound_ThrowsNotFoundException()
    {
        var targetJobId = Guid.NewGuid();
        _jobRepository.Setup(repository => repository.GetByIdAsync(targetJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var command = new ReopenJobCommand(targetJobId);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Job with ID '{targetJobId}' was not found.");
        _jobRepository.Verify(repository => repository.Update(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotOwner_ThrowsForbiddenException()
    {
        var sampleJob = CreateSampleJob(employerId: "different-employer-999");
        sampleJob.MarkAsPublished();
        sampleJob.MarkAsClosed();
        _jobRepository.Setup(repository => repository.GetByIdAsync(sampleJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleJob);

        var command = new ReopenJobCommand(sampleJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("You do not have permission to reopen this job.");
        _jobRepository.Verify(repository => repository.Update(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_JobDeadlineInPast_ThrowsBadRequestException()
    {
        var expiredDeadline = DateTime.UtcNow.AddDays(-2);
        var sampleJob = CreateSampleJob(employerId: "employer-123", expireAt: expiredDeadline);
        sampleJob.MarkAsPublished();
        sampleJob.MarkAsClosed();
        _jobRepository.Setup(repository => repository.GetByIdAsync(sampleJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleJob);

        var command = new ReopenJobCommand(sampleJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cannot reopen a job with an expired deadline. Update the application deadline before reopening.");
        _jobRepository.Verify(repository => repository.Update(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_JobNotClosed_ThrowsBadRequestException()
    {
        var sampleJob = CreateSampleJob(employerId: "employer-123");
        _jobRepository.Setup(repository => repository.GetByIdAsync(sampleJob.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleJob);

        var command = new ReopenJobCommand(sampleJob.Id);
        var handleAction = () => _handler.Handle(command, CancellationToken.None);

        await handleAction.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Only closed jobs can be reopened.");
        _jobRepository.Verify(repository => repository.Update(It.IsAny<Job>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
