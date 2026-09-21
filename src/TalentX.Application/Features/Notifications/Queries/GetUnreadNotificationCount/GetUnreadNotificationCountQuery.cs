using MediatR;

namespace TalentX.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery : IRequest<int>;
