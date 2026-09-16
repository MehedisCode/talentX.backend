using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, PaginatedList<NotificationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetNotificationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PaginatedList<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var talentId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view notifications.");

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var notifications = await _unitOfWork.Notifications.GetByRecipientAsync(
            talentId, page, pageSize, cancellationToken);

        var items = _mapper.Map<List<NotificationDto>>(notifications.Items);

        return new PaginatedList<NotificationDto>(items, notifications.Page, notifications.PageSize, notifications.TotalCount);
    }
}
