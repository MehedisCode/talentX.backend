using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces;

public interface INotificationService
{
    Task<NotificationDto> PublishAsync(
        string recipientUserId,
        NotificationType type,
        string title,
        string body,
        string? metadata,
        CancellationToken cancellationToken);
}
