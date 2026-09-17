using MediatR;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Auth.DeleteAccount;

public sealed class SendDeleteAccountOtpCommandHandler : IRequestHandler<SendDeleteAccountOtpCommand, bool>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;
    private readonly IOtpAttemptTracker _attemptTracker;
    private readonly IOtpCooldownTracker _cooldownTracker;
    private readonly IEmailService _emailService;

    public SendDeleteAccountOtpCommandHandler(
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IOtpService otpService,
        IOtpAttemptTracker attemptTracker,
        IOtpCooldownTracker cooldownTracker,
        IEmailService emailService)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _otpService = otpService;
        _attemptTracker = attemptTracker;
        _cooldownTracker = cooldownTracker;
        _emailService = emailService;
    }

    public async Task<bool> Handle(SendDeleteAccountOtpCommand request, CancellationToken cancellationToken)
    {
        var email = _currentUserService.Email!;
        var identityUserId = _currentUserService.UserId!;

        // Domain row may already be gone when a previous delete deleted the data but
        // failed to remove the identity row. Allow the retry so that flow can converge.
        var user = await _unitOfWork.Users.GetByUserIdAsync(identityUserId, cancellationToken);
        var recipientName = string.IsNullOrWhiteSpace(user?.Name) ? email : user!.Name;

        var otpKey = OtpKeys.DeleteAccount(email);

        if (await _cooldownTracker.IsOnCooldownAsync(otpKey, cancellationToken))
        {
            var remaining = await _cooldownTracker.GetRemainingCooldownSecondsAsync(otpKey, cancellationToken);
            throw new ValidationException(
                $"Please wait {remaining} seconds before requesting a new verification code.");
        }

        var otp = await _otpService.GenerateOtpAsync(otpKey, cancellationToken: cancellationToken);

        // A new code must clear any lockout, otherwise the "request a new code" path is blocked.
        await _attemptTracker.ResetAsync(otpKey, cancellationToken);

        // Policy: OTP delivery is the requested action, so a render/dispatch failure surfaces.
        await _emailService.SendOtpEmailAsync(email, recipientName, otp, cancellationToken);

        await _cooldownTracker.SetCooldownAsync(otpKey, OtpAttemptPolicy.ResendCooldown, cancellationToken);

        return true;
    }
}
