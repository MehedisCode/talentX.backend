using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Tests.Features.Notifications;

public class GetUnreadNotificationCountQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly GetUnreadNotificationCountQueryHandler _handler;

    public GetUnreadNotificationCountQueryHandlerTests()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns("talent-user-1");
        _unitOfWork.SetupGet(unit => unit.Notifications).Returns(_notificationRepository.Object);

        _handler = new GetUnreadNotificationCountQueryHandler(_unitOfWork.Object, _currentUserService.Object);
    }

    [Fact]
    public async Task Handle_ValidUser_ReturnsUnreadCount()
    {
        _notificationRepository
            .Setup(repository => repository.GetUnreadCountAsync("talent-user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var result = await _handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        result.Should().Be(4);
        _notificationRepository.Verify(
            repository => repository.GetUnreadCountAsync("talent-user-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedException()
    {
        _currentUserService.SetupGet(service => service.UserId).Returns((string?)null);

        var action = () => _handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>();
    }
}
