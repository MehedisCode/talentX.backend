using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;

public sealed class SendBatchInterviewInvitationsCommandHandler
    : IRequestHandler<SendBatchInterviewInvitationsCommand, SendBatchInterviewInvitationsResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendBatchInterviewInvitationsCommandHandler> _logger;

    public SendBatchInterviewInvitationsCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<SendBatchInterviewInvitationsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<SendBatchInterviewInvitationsResult> Handle(
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

        var results = new List<CandidateInvitationResultDto>();
        var successfulInvitations = new List<(InterviewInvitation Invitation, JobApplication Application)>();

        foreach (var applicationId in request.ApplicationIds)
        {
            var application = await _unitOfWork.JobApplications.GetByIdAsync(applicationId, cancellationToken);
            if (application is null || application.JobId != request.JobId)
            {
                results.Add(new CandidateInvitationResultDto(
                    applicationId,
                    "Unknown Candidate",
                    "",
                    Success: false,
                    InvitationId: null,
                    SkipReason: "Job application was not found or does not belong to this job.",
                    CanForceSend: false,
                    HasDanglingPendingInvite: false));
                continue;
            }

            var candidateName = string.IsNullOrWhiteSpace(application.ApplicantName) ? "Candidate" : application.ApplicantName;
            var candidateEmail = application.ApplicantEmail ?? "";

            if (string.IsNullOrWhiteSpace(application.TalentUserId))
            {
                // No registered talent account to invite into the portal (e.g. a guest
                // application awaiting email verification) — nothing to link the invitation to.
                results.Add(new CandidateInvitationResultDto(
                    applicationId,
                    candidateName,
                    candidateEmail,
                    Success: false,
                    InvitationId: null,
                    SkipReason: "Candidate does not have a registered talent account to receive portal invitations.",
                    CanForceSend: false,
                    HasDanglingPendingInvite: false));
                continue;
            }

            // Looked up once, up front, so a dangling pending invite can be reported alongside
            // whichever skip reason ultimately fires (e.g. a Rejected application that also has
            // a live pending invite) rather than only when it is the primary reason.
            var existingInvite = await _unitOfWork.InterviewInvitations.GetByApplicationIdAsync(applicationId, cancellationToken);
            var hasDanglingPendingInvite = existingInvite?.Status == InterviewInvitationStatus.Pending;

            if (application.Status == ApplicationStatus.Hired)
            {
                // Hired is deliberately never overridable, even with Force — re-inviting a hired
                // candidate doesn't make sense as a workflow.
                results.Add(new CandidateInvitationResultDto(
                    applicationId,
                    candidateName,
                    candidateEmail,
                    Success: false,
                    InvitationId: null,
                    SkipReason: $"Candidate application is already resolved ({application.Status}).",
                    CanForceSend: false,
                    HasDanglingPendingInvite: hasDanglingPendingInvite));
                continue;
            }

            if (application.Status == ApplicationStatus.Rejected)
            {
                if (!request.Force)
                {
                    // Already resolved; don't let a re-invite silently overwrite a rejection
                    // decision unless the employer explicitly forces it.
                    results.Add(new CandidateInvitationResultDto(
                        applicationId,
                        candidateName,
                        candidateEmail,
                        Success: false,
                        InvitationId: null,
                        SkipReason: $"Candidate application is already resolved ({application.Status}).",
                        CanForceSend: true,
                        HasDanglingPendingInvite: hasDanglingPendingInvite));
                    continue;
                }

                if (hasDanglingPendingInvite)
                {
                    existingInvite!.Cancel();
                }
            }
            else if (hasDanglingPendingInvite)
            {
                if (!request.Force)
                {
                    // A pending invitation already exists for this application; don't send a
                    // duplicate unless the employer explicitly forces it.
                    results.Add(new CandidateInvitationResultDto(
                        applicationId,
                        candidateName,
                        candidateEmail,
                        Success: false,
                        InvitationId: null,
                        SkipReason: "Candidate already has a pending interview invitation for this position.",
                        CanForceSend: true,
                        HasDanglingPendingInvite: false));
                    continue;
                }

                existingInvite!.Cancel();
            }

            var invitation = new InterviewInvitation(
                request.JobId,
                applicationId,
                userId,
                application.TalentUserId!,
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
            var previousStatus = application.Status;
            application.UpdateStatus(ApplicationStatus.Invited);
            await _unitOfWork.JobApplicationStatusHistories.AddAsync(
                JobApplicationStatusHistory.Create(application.Id, previousStatus, ApplicationStatus.Invited, userId),
                cancellationToken);

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

            results.Add(new CandidateInvitationResultDto(
                applicationId,
                candidateName,
                candidateEmail,
                Success: true,
                InvitationId: invitation.Id,
<<<<<<< HEAD
                SkipReason: null,
                CanForceSend: false,
                HasDanglingPendingInvite: false));
=======
                SkipReason: null));

            successfulInvitations.Add((invitation, application));
>>>>>>> e06d5a7beb5b31a499b701afcabd54c3c2976bf0
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var (invitation, application) in successfulInvitations)
        {
            if (string.IsNullOrWhiteSpace(application.TalentUserId))
            {
                continue;
            }

            try
            {
                var metadata = JsonSerializer.Serialize(new
                {
                    invitationId = invitation.Id,
                    jobId = job.Id,
                    jobTitle = job.Title,
                    companyName = job.CompanyName
                });

                await _notificationService.PublishAsync(
                    new PublishNotificationParameters(
                        RecipientUserId: application.TalentUserId,
                        Type: NotificationType.InterviewInvitationReceived,
                        Title: "Interview Invitation",
                        Body: $"{job.CompanyName} invited you to interview for {job.Title}.",
                        Metadata: metadata),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to publish interview invitation notification for invitation {InvitationId}",
                    invitation.Id);
            }
        }

        var invitedCount = results.Count(r => r.Success);
        var skippedCount = results.Count(r => !r.Success);

        return new SendBatchInterviewInvitationsResult(
            TotalProcessed: results.Count,
            InvitedCount: invitedCount,
            SkippedCount: skippedCount,
            Results: results);
    }
}
