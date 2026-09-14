using MediatR;
using TalentX.Application.Features.JobAlerts.DTOs;

namespace TalentX.Application.Features.NotificationPreferences.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery : IRequest<TalentNotificationPreferenceDto>;
