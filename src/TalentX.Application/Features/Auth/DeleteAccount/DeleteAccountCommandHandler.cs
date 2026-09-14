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

        // 1. Verify the OTP first — no mutation on failure.
        //    A blocked key (too many failed attempts) refuses verification until a new
        //    code is requested; requesting one resets the attempt counter.
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

        // 2. Load the domain user — source of role and name; also the idempotency gate.
        //    Convergence: if the domain rows are already gone (a prior attempt died between
        //    step 7 and step 8) but the identity row remains, finish the identity delete
        //    instead of stranding a sign-in-able account with no profile.
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

        // 3. Employer branch — remove ALL their jobs (any status). The Job FK cascades
        //    (applications, saved jobs, invitations, interview invitations + timeslots,
        //    talent matches) are emitted by SaveChanges for the removed jobs.
        if (user.Role == UserRole.Employer)
        {
            var jobs = await _unitOfWork.Jobs.GetByEmployerIdAsync(identityUserId, cancellationToken);
            _unitOfWork.Jobs.RemoveRange(jobs);
        }

        // 4. Talent-side referencing rows. These string columns have no FK constraints,
        //    so they must be removed explicitly or they would orphan permanently.
        await _unitOfWork.JobApplications.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.InterviewInvitations.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.Invitations.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.JobTalentMatches.DeleteByTalentUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.SavedJobs.DeleteByTalentIdAsync(identityUserId, cancellationToken);

        // 5. Directly-owned profile rows (both roles).
        await _unitOfWork.Resumes.DeleteByUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.WorkExperiences.DeleteByUserIdAsync(identityUserId, cancellationToken);
        await _unitOfWork.Educations.DeleteByUserIdAsync(identityUserId, cancellationToken);

        // 6. Domain user — TPT cascades the Talent/Employer child row. Tracked delete so
        //    the audit interceptor records "Delete · User" (the admin-visible marker).
        _unitOfWork.Users.Delete(user);

        // 7. Persist the domain side. The audit interceptor writes the Delete · {Entity}
        //    trail (actor = the user's email) that serves the admin-reflection requirement.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Identity hard delete. UserManager shares this request's scoped ApplicationDbContext,
        //    so the pending ApplicationUser (and cascaded RefreshTokens / AspNet* rows) delete
        //    is committed here, completing the same logical operation. If this fails, the
        //    identity row survives: the user can still sign in, the delete endpoints 404 on the
        //    missing domain user (step 2), and a retried delete converges via step 2.5 below.
        var identityDelete = await _identityService.DeleteApplicationUserAsync(identityUserId);
        if (!identityDelete.Succeeded)
        {
            _logger.LogError(
                "Account deletion for {Email}: domain rows deleted but identity delete failed: {Errors}",
                email, string.Join("; ", identityDelete.Errors));
            throw new ValidationException(
                "Your account data was deleted but sign-in cleanup failed. Please try deleting again to finish.");
        }

        // 9. Belt-and-braces token revoke (refresh rows are already cascaded away).
        await _tokenService.RevokeAllForUserAsync(identityUserId);

        // 10. Confirmation email (D5) — non-fatal; the account is already gone.
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
