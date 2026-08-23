using FluentAssertions;
using FluentValidation;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandHandlerTests
{
    private readonly Mock<IJobRepository> _jobRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpdateJobCommandHandler _handler;

    public UpdateJobCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("employer-1");
        _handler = new UpdateJobCommandHandler(_jobRepository.Object, _unitOfWork.Object, _currentUserService.Object);
    }

    private static Job ExistingDraftJob()
    {
        return Job.Create(
            "employer-1",
            "Backend Engineer",
            "Acme Corp",
            "Build and maintain backend services.",
            EmploymentType.FullTime,
            ExperienceLevel.MidLevel,
            "Dhaka",
            WorkArrangement.Remote,
            new List<string> { "C#" },
            2,
            DateTime.UtcNow.AddDays(30)
        );
    }

    private static UpdateJobCommand CommandFor(Guid id, bool publish = false) => new(
        id, "New Title", null, null, null, null, null, null, null, null, null, publish
    );

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        var act = () => _handler.Handle(CommandFor(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenJobDoesNotExist_DoesNotSave()
    {
        var id = Guid.NewGuid();
        _jobRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Job?)null);

        try
        {
            await _handler.Handle(CommandFor(id), CancellationToken.None);
        }
        catch (NotFoundException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheOwningEmployer_ThrowsForbiddenException()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserService.SetupGet(u => u.UserId).Returns("some-other-employer");

        var act = () => _handler.Handle(CommandFor(job.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheOwningEmployer_DoesNotSave()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _currentUserService.SetupGet(u => u.UserId).Returns("some-other-employer");

        try
        {
            await _handler.Handle(CommandFor(job.Id), CancellationToken.None);
        }
        catch (ForbiddenException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJobExists_AppliesUpdatesAndSaves()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await _handler.Handle(CommandFor(job.Id), CancellationToken.None);

        job.Title.Should().Be("New Title");
        _jobRepository.Verify(r => r.Update(job), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndCompleteData_PublishesTheJob()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await _handler.Handle(CommandFor(job.Id, publish: true), CancellationToken.None);

        job.Status.Should().Be(JobStatus.Published);
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndIncompleteData_ThrowsValidationException()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Force the existing job into an unpublishable state via the update itself.
        var invalidCommand = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, 0, null, true
        );

        var act = () => _handler.Handle(invalidCommand, CancellationToken.None);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task Handle_WithPublishTrueAndIncompleteData_DoesNotSave()
    {
        var job = ExistingDraftJob();
        _jobRepository.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var invalidCommand = new UpdateJobCommand(
            job.Id, null, null, null, null, null, null, null, null, 0, null, true
        );

        try
        {
            await _handler.Handle(invalidCommand, CancellationToken.None);
        }
        catch (FluentValidation.ValidationException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        var act = () => _handler.Handle(CommandFor(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_DoesNotSave()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);

        try
        {
            await _handler.Handle(CommandFor(Guid.NewGuid()), CancellationToken.None);
        }
        catch (UnauthorizedException)
        {
            // expected — assertion below verifies no save happened.
        }

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
