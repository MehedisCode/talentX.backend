using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Commands.RespondToInvitationByToken;

public sealed class RespondToInvitationByTokenCommandHandler
    : IRequestHandler<RespondToInvitationByTokenCommand, JobInvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public RespondToInvitationByTokenCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<JobInvitationDto> Handle(
        RespondToInvitationByTokenCommand request,
        CancellationToken cancellationToken)
    {
        var invitation = await _unitOfWork.Invitations.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new NotFoundException("Invitation not found for the provided token.");

        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            throw new BadRequestException("This invitation has expired.");
        }

        if (request.Accept)
        {
            invitation.Accept();
        }
        else
        {
            invitation.Decline();
        }

        _unitOfWork.Invitations.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
