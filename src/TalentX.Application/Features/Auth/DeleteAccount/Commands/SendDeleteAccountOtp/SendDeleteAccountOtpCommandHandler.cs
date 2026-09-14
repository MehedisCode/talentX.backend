using MediatR;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Auth.DeleteAccount.Commands.SendDeleteAccountOtp;

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

        var user = await _userRepository.GetByUserIdAsync(_currentUserService.UserId!)
            ?? throw new NotFoundException("User not found.");

        await _otpService.ConsumeEmailVerificationAsync(email, cancellationToken);

        var otp = await _otpService.GenerateOtpAsync(
            OtpKeys.DeleteAccount(email), cancellationToken: cancellationToken);

        await _emailService.SendOtpEmailAsync(email, user.Name, otp, cancellationToken);

        return true;
    }
}
