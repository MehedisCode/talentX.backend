using TalentX.Application.Features.Notifications.DTOs;

namespace TalentX.Application.Common.Interfaces;

public interface INotificationBroadcaster
{
    Task PublishToUserAsync(string recipientUserId, NotificationDto notification, CancellationToken cancellationToken);
}
