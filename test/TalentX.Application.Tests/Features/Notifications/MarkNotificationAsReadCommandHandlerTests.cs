using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Tests.Features.Notifications;

public class MarkNotificationAsReadCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly MarkNotificationAsReadCommandHandler _handler;

    public MarkNotificationAsReadCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.Notifications).Returns(_notificationRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new MarkNotificationAsReadCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_MarksNotificationAsRead()
    {
        var notification = Notification.Create(
            "talent-user-1",
            NotificationType.JobAlertMatch,
            "New match",
            "A job matches your alert",
            metadata: null);

        _notificationRepository
            .Setup(repository => repository.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var command = new MarkNotificationAsReadCommand(notification.Id);

        await _handler.Handle(command, CancellationToken.None);

        notification.ReadAt.Should().NotBeNull();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ThrowsNotFoundException()
    {
        var notificationId = Guid.NewGuid();
        _notificationRepository
            .Setup(repository => repository.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        var command = new MarkNotificationAsReadCommand(notificationId);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerDoesNotOwnNotification_ThrowsForbiddenException()
    {
        var notification = Notification.Create(
            "different-talent-user",
            NotificationType.JobAlertMatch,
            "New match",
            "A job matches your alert",
            metadata: null);

        _notificationRepository
            .Setup(repository => repository.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var command = new MarkNotificationAsReadCommand(notification.Id);

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>();
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var command = new MarkNotificationAsReadCommand(Guid.NewGuid());

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
