using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews.Commands.AcceptInterviewInvitation;

public sealed class AcceptInterviewInvitationCommandHandler
    : IRequestHandler<AcceptInterviewInvitationCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;

    public AcceptInterviewInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
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

        await StageConfirmationEmailAsync(invitation, application, scheduledAt, bookedMeetingUrl, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task StageConfirmationEmailAsync(
        InterviewInvitation invitation,
        Domain.Entities.JobApplication? application,
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
        var recipientName = application?.ApplicantName ?? "Candidate";
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
