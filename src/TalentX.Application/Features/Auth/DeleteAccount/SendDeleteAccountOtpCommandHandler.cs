using MediatR;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Auth.DeleteAccount;

public sealed class SendDeleteAccountOtpCommandHandler : IRequestHandler<SendDeleteAccountOtpCommand, bool>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;

    public SendDeleteAccountOtpCommandHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IOtpService otpService,
        IEmailService emailService)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _otpService = otpService;
        _emailService = emailService;
    }

    public async Task<bool> Handle(SendDeleteAccountOtpCommand request, CancellationToken cancellationToken)
    {
        var email = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to request a verification code.");
        var identityUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to request a verification code.");

        var user = await _userRepository.GetByUserIdAsync(identityUserId)
            ?? throw new NotFoundException("User not found.");

        var otpKey = OtpKeys.DeleteAccount(email);

        var otp = await _otpService.GenerateOtpAsync(otpKey, cancellationToken: cancellationToken);

        // Must clear the lockout: the lockout message tells the user to request a
        // new code, and that path is blocked while the counter is still at the max.
        DeleteAccountOtpAttemptGuard.Reset(otpKey);

        await _emailService.SendOtpEmailAsync(email, user.Name, otp, cancellationToken);

        return true;
    }
}
