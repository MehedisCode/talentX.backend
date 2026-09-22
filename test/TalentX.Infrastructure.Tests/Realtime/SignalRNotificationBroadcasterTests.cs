using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Realtime;
using Xunit;

namespace TalentX.Infrastructure.Tests.Realtime;

public class SignalRNotificationBroadcasterTests
{
    private readonly Mock<IHubContext<NotificationsHub>> _hubContext = new();
    private readonly Mock<IHubClients> _hubClients = new();
    private readonly Mock<IClientProxy> _clientProxy = new();
    private readonly Mock<ILogger<SignalRNotificationBroadcaster>> _logger = new();
    private readonly SignalRNotificationBroadcaster _broadcaster;

    public SignalRNotificationBroadcasterTests()
    {
        _hubContext.SetupGet(context => context.Clients).Returns(_hubClients.Object);
        _hubClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_clientProxy.Object);

        _broadcaster = new SignalRNotificationBroadcaster(_hubContext.Object, _logger.Object);
    }

    private static NotificationDto CreateNotificationDto() => new(
        Guid.NewGuid(),
        NotificationType.JobAlertMatch,
        "New match",
        "A job matches your alert",
        null,
        null,
        DateTime.UtcNow);

    [Fact]
    public async Task PublishToUserAsync_ValidRecipient_SendsToCorrectGroup()
    {
        var notification = CreateNotificationDto();

        await _broadcaster.PublishToUserAsync("talent-user-1", notification, CancellationToken.None);

        _hubClients.Verify(clients => clients.Group("talent-user-1"), Times.Once);
        _clientProxy.Verify(
            proxy => proxy.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(arguments => arguments.Length == 1 && Equals(arguments[0], notification)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishToUserAsync_WhenSendThrows_DoesNotPropagate()
    {
        _clientProxy
            .Setup(proxy => proxy.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Hub send failure"));

        var action = () => _broadcaster.PublishToUserAsync(
            "talent-user-1", CreateNotificationDto(), CancellationToken.None);

        await action.Should().NotThrowAsync();
    }
}
