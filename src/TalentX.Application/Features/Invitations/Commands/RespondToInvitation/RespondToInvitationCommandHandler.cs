using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Invitations.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Invitations.Commands.RespondToInvitation;

public sealed class RespondToInvitationCommandHandler : IRequestHandler<RespondToInvitationCommand, JobInvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RespondToInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<JobInvitationDto> Handle(RespondToInvitationCommand request, CancellationToken cancellationToken)
    {
        var talentUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to respond to an invitation.");

        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException($"Invitation '{request.InvitationId}' was not found.");

        if (invitation.TalentUserId != talentUserId)
        {
            throw new ForbiddenException("You do not have permission to respond to this invitation.");
        }

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
