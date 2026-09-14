using MediatR;

namespace TalentX.Application.Features.NotificationPreferences.Commands.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(bool JobAlertsEmailEnabled) : IRequest;
