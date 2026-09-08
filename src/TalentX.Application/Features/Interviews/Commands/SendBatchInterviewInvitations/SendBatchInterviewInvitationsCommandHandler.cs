using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;

public sealed class SendBatchInterviewInvitationsCommandHandler
    : IRequestHandler<SendBatchInterviewInvitationsCommand, IReadOnlyList<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;

    public SendBatchInterviewInvitationsCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
    }

    public async Task<IReadOnlyList<Guid>> Handle(
        SendBatchInterviewInvitationsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to send interview invitations.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken)
            ?? throw new NotFoundException("Job with ID was not found.");

        if (job.EmployerId != userId)
        {
            throw new ForbiddenException("You cannot send interview invitations for jobs you do not own.");
        }

        var createdInvitationIds = new List<Guid>();

        foreach (var applicationId in request.ApplicationIds)
        {
            var application = await _unitOfWork.JobApplications.GetByIdAsync(applicationId, cancellationToken);
            if (application is null || application.JobId != request.JobId)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(application.TalentUserId))
            {
                // No registered talent account to invite into the portal (e.g. a guest
                // application awaiting email verification) — nothing to link the invitation to.
                continue;
            }

            var existingInvite = await _unitOfWork.InterviewInvitations.GetByApplicationIdAsync(applicationId, cancellationToken);
            if (existingInvite is not null && existingInvite.Status == InterviewInvitationStatus.Pending)
            {
                // A pending invitation already exists for this application; don't send a duplicate.
                continue;
            }

            var invitation = new InterviewInvitation(
                request.JobId,
                applicationId,
                userId,
                application.TalentUserId ?? application.ApplicantEmail,
                request.Type,
                request.AssessmentUrl,
                request.AssessmentDeadline,
                request.LocationOrLink,
                request.Instructions);

            if (request.Timeslots is { Count: > 0 })
            {
                foreach (var timeslot in request.Timeslots)
                {
                    invitation.AddTimeslot(timeslot.StartTime, timeslot.EndTime, timeslot.MeetingUrl ?? request.LocationOrLink);
                }
            }

            await _unitOfWork.InterviewInvitations.AddAsync(invitation, cancellationToken);
            createdInvitationIds.Add(invitation.Id);

            if (!string.IsNullOrWhiteSpace(application.ApplicantEmail))
            {
                var subject = $"New Interview Invitation: {job.Title} at {job.CompanyName}";
                var htmlBody = await _emailService.RenderInterviewInvitationEmailAsync(
                    application.ApplicantName, job.Title, job.CompanyName, cancellationToken);

                await _unitOfWork.EmailDispatchLogs.AddAsync(
                    new EmailDispatchLog(
                        application.ApplicantEmail,
                        application.ApplicantName,
                        subject,
                        htmlBody,
                        "InterviewInvitation"),
                    cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return createdInvitationIds;
    }
}
