using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Notifications.DTOs;

namespace TalentX.Infrastructure.Realtime;

public sealed class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private const string ReceiveNotificationMethodName = "ReceiveNotification";

    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ILogger<SignalRNotificationBroadcaster> _logger;

    public SignalRNotificationBroadcaster(
        IHubContext<NotificationsHub> hubContext,
        ILogger<SignalRNotificationBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishToUserAsync(
        string recipientUserId,
        NotificationDto notification,
        CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients
                .Group(recipientUserId)
                .SendAsync(ReceiveNotificationMethodName, notification, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send SignalR notification {NotificationId} to group {RecipientUserId}",
                notification.Id,
                recipientUserId);
        }
    }
}
