using MediatR;

namespace TalentX.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand : IRequest<Unit>;
