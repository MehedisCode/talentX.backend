using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Invitations.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Invitations.Commands.WithdrawInvitation;

public sealed class WithdrawInvitationCommandHandler : IRequestHandler<WithdrawInvitationCommand, JobInvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public WithdrawInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<JobInvitationDto> Handle(WithdrawInvitationCommand request, CancellationToken cancellationToken)
    {
        var employerId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in as an employer to withdraw an invitation.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException($"Job '{request.JobId}' was not found.");

        if (job.EmployerId != employerId)
        {
            throw new ForbiddenException("You do not have permission to manage invitations for this job.");
        }

        var invitation = await _unitOfWork.Invitations.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException($"Invitation '{request.InvitationId}' was not found.");

        if (invitation.JobId != request.JobId)
        {
            throw new BadRequestException("Invitation does not belong to the specified job.");
        }

        invitation.Withdraw();
        _unitOfWork.Invitations.Update(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(invitation.TalentUserId, cancellationToken);

        return new JobInvitationDto
        {
            Id = invitation.Id,
            JobId = invitation.JobId,
            JobTitle = job.Title,
            CompanyName = job.CompanyName ?? "TalentX Employer",
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
