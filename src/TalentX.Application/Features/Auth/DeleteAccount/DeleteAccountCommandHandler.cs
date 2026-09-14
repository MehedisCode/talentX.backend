using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Auth.DeleteAccount;

public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, DeleteAccountResult>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly ILogger<DeleteAccountCommandHandler> _logger;

    public DeleteAccountCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ITokenService tokenService,
        IOtpService otpService,
        IEmailService emailService,
        ILogger<DeleteAccountCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _tokenService = tokenService;
        _otpService = otpService;
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

        if (DeleteAccountOtpAttemptGuard.IsBlocked(otpKey))
        {
            throw new ValidationException(
                "Too many incorrect verification attempts. Request a new code and try again.");
        }

        if (!await _otpService.VerifyOtpAsync(otpKey, request.Otp, cancellationToken))
        {
            DeleteAccountOtpAttemptGuard.RecordFailure(otpKey);
            throw new ValidationException("Invalid or expired verification code.");
        }

        DeleteAccountOtpAttemptGuard.Reset(otpKey);

        var user = await _unitOfWork.Users.GetByUserIdAsync(identityUserId);
        if (user is null)
        {
            var cleanup = await _identityService.DeleteApplicationUserAsync(identityUserId);
            if (!cleanup.Succeeded)
            {
                _logger.LogError(
                    "Account deletion retry for {Email}: domain rows already gone and identity cleanup failed: {Errors}",
                    email, string.Join("; ", cleanup.Errors));
            }

            return new DeleteAccountResult(email);
        }

        if (user.Role == UserRole.Employer)
        {
            var jobs = await _unitOfWork.Jobs.GetByEmployerIdAsync(identityUserId, cancellationToken);
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

        var identityDelete = await _identityService.DeleteApplicationUserAsync(identityUserId);
        if (!identityDelete.Succeeded)
        {
            _logger.LogError(
                "Account deletion for {Email}: domain rows deleted but identity delete failed: {Errors}",
                email, string.Join("; ", identityDelete.Errors));
            throw new ValidationException(
                "Your account data was deleted but sign-in cleanup failed. Please try deleting again to finish.");
        }

        await _tokenService.RevokeAllForUserAsync(identityUserId);

        try
        {
            await _emailService.SendAccountDeletedEmailAsync(email, user.Name, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account deleted for {Email} but the confirmation email failed.", email);
        }

        _logger.LogInformation(
            "Account for {Email} ({Role}) permanently deleted. Audit trail recorded via interceptor.",
            email, user.Role);

        return new DeleteAccountResult(email);
    }
}
