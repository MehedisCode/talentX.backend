using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Auth.DeleteAccount;

public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, DeleteAccountResult>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IOtpService _otpService;
    private readonly IOtpAttemptTracker _attemptTracker;
    private readonly IEmailService _emailService;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        IOtpService otpService,
        IOtpAttemptTracker attemptTracker,
        IEmailService emailService,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _otpService = otpService;
        _attemptTracker = attemptTracker;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DeleteAccountResult> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var email = _currentUserService.Email!;
        var identityUserId = _currentUserService.UserId!;

        var otpKey = OtpKeys.DeleteAccount(email);

        if (await _attemptTracker.IsBlockedAsync(otpKey, cancellationToken))
        {
            throw new ValidationException(
                "Too many incorrect verification attempts. Request a new code and try again.");
        }

        if (!await _otpService.VerifyOtpAsync(otpKey, request.Otp, cancellationToken))
        {
            await _attemptTracker.RecordFailureAsync(otpKey, cancellationToken);
            throw new ValidationException("Invalid or expired verification code.");
        }

        await _attemptTracker.ResetAsync(otpKey, cancellationToken);

        var user = await _unitOfWork.Users.GetByUserIdAsync(identityUserId, cancellationToken);
        var recipientName = string.IsNullOrWhiteSpace(user?.Name) ? email : user!.Name;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (user is not null)
            {
                await DeleteOwnedDomainDataAsync(user, identityUserId, cancellationToken);
            }

            var identityDelete = await _identityService.DeleteApplicationUserAsync(identityUserId);
            if (!identityDelete.Succeeded)
            {
                _logger.LogError(
                    "Account deletion for {Email}: identity delete failed: {Errors}",
                    email, string.Join("; ", identityDelete.Errors));
                throw new BadGatewayException(
                    "Your account deletion could not be completed. Please try again.");
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        try
        {
            await _emailService.SendAccountDeletedEmailAsync(email, recipientName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account deleted for {Email} but the confirmation email failed.", email);
        }

        _logger.LogInformation(
            "Account for {Email} permanently deleted. Audit trail recorded via interceptor.",
            email);

        return new DeleteAccountResult(email);
    }

    private async Task DeleteOwnedDomainDataAsync(User user, string identityUserId, CancellationToken cancellationToken)
    {
        if (user.Role == UserRole.Employer)
        {
            var jobs = await _unitOfWork.Jobs.GetByEmployerIdIncludingDeletedAsync(identityUserId, cancellationToken);
            if (jobs.Count > 0)
            {
                var jobIds = jobs.Select(j => j.Id).ToList();
                await _unitOfWork.JobModerationHistories.DeleteByJobIdsAsync(jobIds, cancellationToken);
                _unitOfWork.Jobs.RemoveRange(jobs);
            }

            await _unitOfWork.Companies.DeleteByEmployerIdAsync(identityUserId, cancellationToken);
        }
        else if (user.Role == UserRole.Talent)
        {
            await _unitOfWork.JobApplications.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.InterviewInvitations.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.Invitations.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.JobTalentMatches.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.SavedJobs.DeleteByTalentIdAsync(identityUserId, cancellationToken);

            await _unitOfWork.JobAlerts.DeleteByTalentIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.TalentNotificationPreferences.DeleteByTalentIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.JobAlertNotificationHistories.DeleteByTalentIdAsync(identityUserId, cancellationToken);

            await _unitOfWork.Resumes.DeleteByUserIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.WorkExperiences.DeleteByUserIdAsync(identityUserId, cancellationToken);
            await _unitOfWork.Educations.DeleteByUserIdAsync(identityUserId, cancellationToken);
        }

        // Both roles receive in-app notifications; RecipientUserId has no FK, so nothing cascades.
        await _unitOfWork.Notifications.DeleteByRecipientIdAsync(identityUserId, cancellationToken);

        _unitOfWork.Users.Delete(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
