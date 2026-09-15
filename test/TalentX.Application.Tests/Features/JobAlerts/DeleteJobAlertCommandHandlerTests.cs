using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.JobAlerts.Commands.DeleteJobAlert;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Tests.Features.JobAlerts;

public class DeleteJobAlertCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IJobAlertRepository> _jobAlertRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly DeleteJobAlertCommandHandler _handler;

    public DeleteJobAlertCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.JobAlerts).Returns(_jobAlertRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteJobAlertCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesAlert()
    {
        var existingAlert = JobAlert.Create(
            "talent-user-1",
            "Name",
            "Title",
            null,
            null,
            null,
            null);

        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(existingAlert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAlert);

        var command = new DeleteJobAlertCommand(existingAlert.Id);

        await _handler.Handle(command, CancellationToken.None);

        _jobAlertRepository.Verify(repository => repository.Remove(existingAlert), Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlertNotFound_ThrowsNotFoundException()
    {
        var alertId = Guid.NewGuid();
        _jobAlertRepository
            .Setup(repository => repository.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobAlert?)null);

        var command = new DeleteJobAlertCommand(alertId);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _jobAlertRepository.Verify(repository => repository.Remove(It.IsAny<JobAlert>()), Times.Never);
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

        var command = new DeleteJobAlertCommand(existingAlert.Id);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>();
        _jobAlertRepository.Verify(repository => repository.Remove(It.IsAny<JobAlert>()), Times.Never);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new DeleteJobAlertCommand(Guid.NewGuid());

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
