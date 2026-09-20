using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Tests.Features.Notifications;

public class MarkAllNotificationsAsReadCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly MarkAllNotificationsAsReadCommandHandler _handler;

    public MarkAllNotificationsAsReadCommandHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.Notifications).Returns(_notificationRepository.Object);
        _unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new MarkAllNotificationsAsReadCommandHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_MarksAllNotificationsAsReadAndSaves()
    {
        _notificationRepository
            .Setup(repository => repository.MarkAllAsReadAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

        _notificationRepository.Verify(
            repository => repository.MarkAllAsReadAsync("talent-user-1", It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var action = () => _handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
        _notificationRepository.Verify(
            repository => repository.MarkAllAsReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
