using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobAlerts.Commands.UpdateJobAlert;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class UpdateJobAlertCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly UpdateJobAlertCommandHandler _handler;

    public UpdateJobAlertCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.JobAlerts).Returns(_jobAlertRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateJobAlertCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesAlert()
    {
        var existingAlert = JobAlert.Create(
            "talent-user-1",
            "Old Name",
            "Old Title",
            "Old Location",
            EmploymentType.Contract,
            ExperienceLevel.EntryLevel,
            WorkArrangement.OnSite);

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(existingAlert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAlert);

        var updateParameters = new UpdateJobAlertParameters(
            Name: "New Name",
            Title: "Lead Engineer",
            Location: "Remote",
            EmploymentType: EmploymentType.FullTime,
            ExperienceLevel: ExperienceLevel.Lead,
            WorkArrangement: WorkArrangement.FullRemote);

        var command = new UpdateJobAlertCommand(existingAlert.Id, updateParameters);

        await _handler.Handle(command, CancellationToken.None);

        existingAlert.Name.Should().Be("New Name");
        existingAlert.Title.Should().Be("Lead Engineer");
        existingAlert.Location.Should().Be("Remote");
        existingAlert.EmploymentType.Should().Be(EmploymentType.FullTime);
        existingAlert.ExperienceLevel.Should().Be(ExperienceLevel.Lead);
        existingAlert.WorkArrangement.Should().Be(WorkArrangement.FullRemote);
        existingAlert.UpdatedAt.Should().NotBeNull();

        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlertNotFound_ThrowsNotFoundException()
    {
        var alertId = Guid.NewGuid();
        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobAlert?)null);

        var command = new UpdateJobAlertCommand(alertId, new UpdateJobAlertParameters("Name", "Title"));

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerDoesNotOwnAlert_ThrowsForbiddenException()
    {
        var existingAlert = JobAlert.Create(
            "different-talent-user",
            "Name",
            "Title",
            null,
            null,
            null,
            null);

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(existingAlert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAlert);

        var command = new UpdateJobAlertCommand(existingAlert.Id, new UpdateJobAlertParameters("New Name", "New Title"));

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new UpdateJobAlertCommand(Guid.NewGuid(), new UpdateJobAlertParameters("Name", "Title"));

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
