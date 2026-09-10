using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Invitations.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Invitations.Queries.GetMyInvitations;

public sealed class GetMyInvitationsQueryHandler
    : IRequestHandler<GetMyInvitationsQuery, List<JobInvitationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMyInvitationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<JobInvitationDto>> Handle(
        GetMyInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var talentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your invitations.");

        var invitations = await _unitOfWork.Invitations.GetByTalentUserIdAsync(talentUserId, cancellationToken);
        if (invitations.Count == 0)
        {
            return new List<JobInvitationDto>();
        }

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(talentUserId, cancellationToken);

        var results = new List<JobInvitationDto>(invitations.Count);
        foreach (var inv in invitations)
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(inv.JobId, cancellationToken);
            results.Add(new JobInvitationDto
            {
                Id = inv.Id,
                JobId = inv.JobId,
                JobTitle = job?.Title ?? "Unknown Position",
                CompanyName = job?.CompanyName ?? "TalentX Employer",
                TalentUserId = inv.TalentUserId,
                TalentName = talent?.Name ?? "Candidate",
                Message = inv.Message,
                InvitedDate = inv.InvitedDate,
                ExpiresAt = inv.ExpiresAt,
                Status = inv.Status,
                IsActive = inv.IsActive
            });
        }

        return results;
    }
}
