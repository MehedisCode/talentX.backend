using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Interviews.Commands.DeclineInterviewInvitation;

public sealed class DeclineInterviewInvitationCommandHandler
    : IRequestHandler<DeclineInterviewInvitationCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeclineInterviewInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        DeclineInterviewInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to decline an interview invitation.");

        var invitation = await _unitOfWork.InterviewInvitations.GetByIdWithTimeslotsAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException("Interview invitation was not found.");

        if (invitation.TalentUserId != userId)
        {
            throw new ForbiddenException("You cannot view or decline interview invitations belonging to another user.");
        }

        InterviewRules.EnsureNotExpired(invitation, DateTime.UtcNow);

        invitation.Decline(request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
