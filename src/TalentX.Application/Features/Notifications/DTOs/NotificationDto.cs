using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Notifications.DTOs;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    string? Metadata,
    DateTime? ReadAt,
    DateTime CreatedAt);
