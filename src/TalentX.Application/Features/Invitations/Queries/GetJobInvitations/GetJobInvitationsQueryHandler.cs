using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Invitations.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Invitations.Queries.GetJobInvitations;

public sealed class GetJobInvitationsQueryHandler
    : IRequestHandler<GetJobInvitationsQuery, List<JobInvitationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetJobInvitationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<JobInvitationDto>> Handle(
        GetJobInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as an employer to view invitations.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to view invitations for this job.");
        }

        var invitations = await _unitOfWork.Invitations.GetByJobIdAsync(job.Id, cancellationToken);
        if (invitations.Count == 0)
        {
            return new List<JobInvitationDto>();
        }

        var allTalents = await _unitOfWork.Talents.GetAllAsync(cancellationToken);
        var talentMap = allTalents.ToDictionary(t => t.UserId);

        return invitations
            .Select(i =>
            {
                talentMap.TryGetValue(i.TalentUserId, out var talent);
                return new JobInvitationDto
                {
                    Id = i.Id,
                    JobId = i.JobId,
                    TalentUserId = i.TalentUserId,
                    TalentName = talent?.Name ?? "Candidate",
                    InvitedDate = i.InvitedDate,
                    IsActive = i.IsActive
                };
            })
            .ToList();
    }
}
