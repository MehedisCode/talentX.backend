using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Queries.GetInvitationByToken;

public sealed class GetInvitationByTokenQueryHandler
    : IRequestHandler<GetInvitationByTokenQuery, JobInvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetInvitationByTokenQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<JobInvitationDto> Handle(
        GetInvitationByTokenQuery request,
        CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new NotFoundException("Invitation not found for the provided token.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(invitation.JobId, cancellationToken);
        var talent = await _unitOfWork.Talents.GetByUserIdAsync(invitation.TalentUserId, cancellationToken);

        return new JobInvitationDto
        {
            Id = invitation.Id,
            JobId = invitation.JobId,
            JobTitle = job?.Title ?? "Unknown Position",
            CompanyName = job?.CompanyName ?? "TalentX Employer",
            TalentUserId = invitation.TalentUserId,
            TalentName = talent?.Name ?? "Candidate",
            Message = invitation.Message,
            InvitedDate = invitation.InvitedDate,
            ExpiresAt = invitation.ExpiresAt,
            Status = invitation.Status,
            IsActive = invitation.IsActive
        };
    }
}
