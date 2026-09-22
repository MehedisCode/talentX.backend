using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.DTOs;

namespace TalentX.Application.Common.Interfaces;

public interface INotificationService
{
    Task<NotificationDto> PublishAsync(
        PublishNotificationParameters parameters,
        CancellationToken cancellationToken);
}
