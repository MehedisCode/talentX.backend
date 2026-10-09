using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Commands.AcceptInterviewInvitation;

public sealed class AcceptInterviewInvitationCommandHandler
    : IRequestHandler<AcceptInterviewInvitationCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AcceptInterviewInvitationCommandHandler> _logger;

    public AcceptInterviewInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<AcceptInterviewInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(
        AcceptInterviewInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to accept an interview invitation.");

        var invitation = await _unitOfWork.InterviewInvitations.GetByIdWithTimeslotsAsync(request.InvitationId, cancellationToken)
            ?? throw new NotFoundException("Interview invitation was not found.");

        if (invitation.TalentUserId != userId)
        {
            throw new ForbiddenException("You cannot view or accept interview invitations belonging to another user.");
        }

        InterviewRules.EnsureNotExpired(invitation, DateTime.UtcNow);
        InterviewRules.EnsureIsPending(invitation);

        DateTime? scheduledAt = null;
        string? bookedMeetingUrl = null;

        if (request.SelectedTimeslotId.HasValue)
        {
            var timeslot = invitation.Timeslots.FirstOrDefault(t => t.Id == request.SelectedTimeslotId.Value)
                ?? throw new NotFoundException("Selected timeslot was not found in this interview invitation.");

            if (timeslot.IsBooked && timeslot.BookedByTalentUserId != userId)
            {
                throw new ConflictException("This timeslot has already been booked by another candidate. Please select a different timeslot.");
            }

            timeslot.Book(userId);
            scheduledAt = timeslot.StartTime;
            bookedMeetingUrl = timeslot.MeetingUrl;
        }

        invitation.Accept(request.SelectedTimeslotId, scheduledAt);

        var application = await _unitOfWork.JobApplications.GetByIdAsync(invitation.JobApplicationId, cancellationToken);
        if (application is not null)
        {
            var previousStatus = application.Status;
            application.UpdateStatus(ApplicationStatus.Interview);
            await _unitOfWork.JobApplicationStatusHistories.AddAsync(
                JobApplicationStatusHistory.Create(application.Id, previousStatus, ApplicationStatus.Interview, userId),
                cancellationToken);
        }

        var candidateDisplayName = await ResolveDisplayNameAsync(application, cancellationToken);

        await StageConfirmationEmailAsync(invitation, application, candidateDisplayName, scheduledAt, bookedMeetingUrl, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var job = await _unitOfWork.Jobs.GetByIdAsync(invitation.JobId, cancellationToken);
            var acceptedCandidateName = candidateDisplayName;
            var jobTitle = job?.Title ?? "Job Position";
            var metadata = JsonSerializer.Serialize(new
            {
                invitationId = invitation.Id,
                jobId = invitation.JobId,
                applicationId = invitation.JobApplicationId,
                candidateName = acceptedCandidateName,
                jobTitle
            });

            await _notificationService.PublishAsync(
                new PublishNotificationParameters(
                    RecipientUserId: invitation.EmployerUserId,
                    Type: NotificationType.InterviewInvitationAccepted,
                    Title: "Interview Accepted",
                    Body: $"{acceptedCandidateName} accepted the interview invitation for {jobTitle}.",
                    Metadata: metadata),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to publish interview accepted notification for invitation {InvitationId}",
                invitation.Id);
        }
    }

    private async Task<string> ResolveDisplayNameAsync(Domain.Entities.JobApplication? application, CancellationToken cancellationToken)
    {
        // Show the talent's CURRENT profile name; the frozen application.ApplicantName is the
        // fallback for guests / missing talent rows.
        if (!string.IsNullOrWhiteSpace(application?.TalentUserId))
        {
            var talent = await _unitOfWork.Talents.GetByUserIdAsync(application.TalentUserId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(talent?.Name))
            {
                return talent!.Name;
            }
        }

        return string.IsNullOrWhiteSpace(application?.ApplicantName) ? "Candidate" : application.ApplicantName;
    }

    private async Task StageConfirmationEmailAsync(
        InterviewInvitation invitation,
        Domain.Entities.JobApplication? application,
        string recipientName,
        DateTime? scheduledAt,
        string? bookedMeetingUrl,
        CancellationToken cancellationToken)
    {
        var recipientEmail = application?.ApplicantEmail ?? _currentUserService.Email;
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            return;
        }

        var job = await _unitOfWork.Jobs.GetByIdAsync(invitation.JobId, cancellationToken);
        var jobTitle = job?.Title ?? "Job Position";
        var companyName = job?.CompanyName ?? "Employer";

        var scheduledTimeOrDeadline = scheduledAt.HasValue
            ? $"{scheduledAt.Value:f} UTC"
            : invitation.AssessmentDeadline.HasValue ? $"{invitation.AssessmentDeadline.Value:f} UTC" : "N/A";

        var locationOrLink = invitation.Type == InterviewType.OnlineAssessment
            ? invitation.AssessmentUrl ?? "Online Portal"
            : bookedMeetingUrl ?? invitation.LocationOrLink ?? "See portal details";

        var subject = $"Confirmed Interview: {jobTitle} at {companyName}";
        var htmlBody = await _emailService.RenderInterviewConfirmationEmailAsync(
            recipientName,
            jobTitle,
            companyName,
            invitation.Type.ToString(),
            locationOrLink,
            scheduledTimeOrDeadline,
            invitation.Instructions,
            cancellationToken);

        await _unitOfWork.EmailDispatchLogs.AddAsync(
            new EmailDispatchLog(recipientEmail, recipientName, subject, htmlBody, "InterviewConfirmation"),
            cancellationToken);
    }
}
