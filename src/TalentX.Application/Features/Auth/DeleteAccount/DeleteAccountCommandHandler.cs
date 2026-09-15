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
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly IOtpAttemptTracker _attemptTracker;
    private readonly IEmailService _emailService;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ITokenService tokenService,
        IOtpService otpService,
        IOtpAttemptTracker attemptTracker,
        IEmailService emailService,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _tokenService = tokenService;
        _otpService = otpService;
        _attemptTracker = attemptTracker;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<DeleteAccountResult> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var email = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to delete your account.");
        var identityUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to delete your account.");

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

        // A null user means a prior attempt deleted the domain data but not the
        // identity row; skip straight to identity cleanup so retries converge.
        var user = await _unitOfWork.Users.GetByUserIdAsync(identityUserId, cancellationToken);
        var recipientName = string.IsNullOrWhiteSpace(user?.Name) ? email : user!.Name;

        if (user is not null)
        {
            await DeleteOwnedDomainDataAsync(user, identityUserId, cancellationToken);
        }

        var identityDelete = await _identityService.DeleteApplicationUserAsync(identityUserId);
        if (!identityDelete.Succeeded)
        {
            _logger.LogError(
                "Account deletion for {Email}: domain rows deleted but identity delete failed: {Errors}",
                email, string.Join("; ", identityDelete.Errors));
            throw new BadGatewayException(
                "Your account data was deleted but sign-in cleanup failed. Request a new code and try again to finish.");
        }

        await _tokenService.RevokeAllForUserAsync(identityUserId);

        // Policy: the account is already gone, so a confirmation-email failure is logged, never thrown.
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
            // Must include soft-deleted jobs: hard delete removes every row the employer owns.
            var jobs = await _unitOfWork.Jobs.GetByEmployerIdIncludingDeletedAsync(identityUserId, cancellationToken);
            _unitOfWork.Jobs.RemoveRange(jobs);
        }

        await _unitOfWork.JobApplications.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.InterviewInvitations.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.Invitations.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.JobTalentMatches.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.SavedJobs.DeleteByTalentIdAsync(identityUserId, cancellationToken);

        await _unitOfWork.Resumes.DeleteByUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.WorkExperiences.DeleteByUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.Educations.DeleteByUserIdAsync(identityUserId, cancellationToken);

        _unitOfWork.Users.Delete(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
