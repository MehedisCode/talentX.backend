using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Queries.GetTalentInvitations;

public sealed class GetTalentInvitationsQueryHandler
    : IRequestHandler<GetTalentInvitationsQuery, PaginatedList<TalentInvitationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetTalentInvitationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<TalentInvitationDto>> Handle(
        GetTalentInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be authenticated to access your invitations.");

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var pagedTalentActiveInvitations = await _unitOfWork.Invitations.GetActiveInvitationsForTalentAsync(
            currentUserId,
            page,
            pageSize,
            cancellationToken);

        var talentInvitations = pagedTalentActiveInvitations.Items.Select(r => new TalentInvitationDto(
            r.Id,
            r.JobId,
            r.JobTitle,
            r.Company,
            r.InvitedDate,
            r.ExpiresAt,
            r.Message,
            r.WorkArrangement,
            r.EmploymentType
        )).ToList();

        return new PaginatedList<TalentInvitationDto>(talentInvitations, pagedTalentActiveInvitations.Page, pagedTalentActiveInvitations.PageSize, pagedTalentActiveInvitations.TotalCount);
    }
}
