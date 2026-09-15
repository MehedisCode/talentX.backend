using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobAlerts.Commands.ToggleJobAlert;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class ToggleJobAlertCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly ToggleJobAlertCommandHandler _handler;

    public ToggleJobAlertCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.JobAlerts).Returns(_jobAlertRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ToggleJobAlertCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_TogglesAlertState()
    {
        var existingAlert = JobAlert.Create(
            "talent-user-1",
            "Name",
            "Title",
            null,
            null,
            null,
            null);
        existingAlert.IsEnabled.Should().BeTrue();

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(existingAlert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAlert);

        var command = new ToggleJobAlertCommand(existingAlert.Id, IsEnabled: false);

        await _handler.Handle(command, CancellationToken.None);

        existingAlert.IsEnabled.Should().BeFalse();
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

        var command = new ToggleJobAlertCommand(alertId, IsEnabled: true);

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

        var command = new ToggleJobAlertCommand(existingAlert.Id, IsEnabled: false);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new ToggleJobAlertCommand(Guid.NewGuid(), IsEnabled: false);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
