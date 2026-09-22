using AutoMapper;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationBroadcaster _notificationBroadcaster;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork unitOfWork,
        INotificationBroadcaster notificationBroadcaster,
        IMapper mapper,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationBroadcaster = notificationBroadcaster;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<NotificationDto> PublishAsync(
        PublishNotificationParameters parameters,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            parameters.RecipientUserId,
            parameters.Type,
            parameters.Title,
            parameters.Body,
            parameters.Metadata);

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var notificationDto = _mapper.Map<NotificationDto>(notification);

        try
        {
            await _notificationBroadcaster.PublishToUserAsync(
                parameters.RecipientUserId,
                notificationDto,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to broadcast notification {NotificationId} to user {RecipientUserId}",
                notification.Id,
                parameters.RecipientUserId);
        }

        return notificationDto;
    }
}
