using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Invitations.Commands.SendJobInvitation;

public sealed class SendJobInvitationCommandHandler
    : IRequestHandler<SendJobInvitationCommand, SendJobInvitationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendJobInvitationCommandHandler> _logger;

    public SendJobInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<SendJobInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
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

        var effectiveStatus = job.GetEffectiveStatus();
        if (effectiveStatus is not (JobStatus.Published or JobStatus.Expired))
        {
            throw new BadRequestException("Invitations can only be sent for active or expired jobs.");
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

        if (!string.IsNullOrWhiteSpace(talent.Email))
        {
            var baseUrl = GetFrontendBaseUrl();
            var jobUrl = $"{baseUrl}/jobs/{job.Id}?invitation={invitation.Token}";

            _logger.LogInformation(
                "Sending job invitation email for job {JobId} to recipient {TalentUserId}",
                job.Id,
                request.TalentUserId);

            await _emailService.SendJobInvitationEmailAsync(
                talent.Email,
                talent.Name ?? "Candidate",
                job.Title,
                job.CompanyName ?? "TalentX Employer",
                jobUrl,
                cancellationToken);
        }

        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                invitationId = invitation.Id,
                jobId = job.Id,
                jobTitle = job.Title,
                companyName = job.CompanyName ?? "TalentX Employer",
                token = invitation.Token
            });

            await _notificationService.PublishAsync(
                new PublishNotificationParameters(
                    RecipientUserId: request.TalentUserId,
                    Type: NotificationType.JobInvitationReceived,
                    Title: "Job Invitation",
                    Body: $"{job.CompanyName ?? "An employer"} invited you to apply for {job.Title}.",
                    Metadata: metadata),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to publish job invitation notification for invitation {InvitationId}",
                invitation.Id);
        }

        return new SendJobInvitationResponse(
            invitation.Id,
            invitation.JobId,
            invitation.TalentUserId,
            invitation.InvitedDate,
            invitation.IsActive);
    }

    private static string GetFrontendBaseUrl()
    {
        var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
        if (!string.IsNullOrWhiteSpace(corsOrigins))
        {
            var firstOrigin = corsOrigins
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstOrigin))
            {
                return firstOrigin.TrimEnd('/');
            }
        }

        return "http://localhost:3000";
    }
}
