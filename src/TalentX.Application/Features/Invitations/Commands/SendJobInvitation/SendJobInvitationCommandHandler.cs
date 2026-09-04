using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Invitations.Commands.SendJobInvitation;

public sealed class SendJobInvitationCommandHandler
    : IRequestHandler<SendJobInvitationCommand, SendJobInvitationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public SendJobInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<SendJobInvitationResponse> Handle(
        SendJobInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as an employer to send invitations.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to send invitations for this job.");
        }

        if (job.Status != JobStatus.Published)
        {
            throw new BadRequestException("Invitations can only be sent for published jobs.");
        }

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(request.TalentUserId, cancellationToken)
            ?? throw new NotFoundException($"Talent '{request.TalentUserId}' was not found.");

        var alreadyInvited = await _unitOfWork.Invitations.ExistsActiveAsync(
            request.JobId, request.TalentUserId, cancellationToken);

        if (alreadyInvited)
        {
            throw new ConflictException("An active invitation has already been sent to this candidate for this job.");
        }

        var invitation = new Invitation(job.Id, request.TalentUserId);
        await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SendJobInvitationResponse(
            invitation.Id,
            invitation.JobId,
            invitation.TalentUserId,
            invitation.InvitedDate,
            invitation.IsActive);
    }
}
