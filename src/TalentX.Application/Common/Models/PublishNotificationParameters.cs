using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Models;

public sealed record PublishNotificationParameters(
    string RecipientUserId,
    NotificationType Type,
    string Title,
    string Body,
    string? Metadata = null);
