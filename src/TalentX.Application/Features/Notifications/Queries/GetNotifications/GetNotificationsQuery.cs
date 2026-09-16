using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.DTOs;

namespace TalentX.Application.Features.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(
    int Page = PaginationRequest.DefaultPageNumber,
    int PageSize = PaginationRequest.DefaultPageSize
) : IRequest<PaginatedList<NotificationDto>>;
