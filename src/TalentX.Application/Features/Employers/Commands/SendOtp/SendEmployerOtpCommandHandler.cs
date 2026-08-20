using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Employers.Commands.SendOtp;

/// <summary>
/// Handler for <see cref="SendEmployerOtpCommand"/> to dispatch a verification OTP email.
/// Supports both new unverified emails and existing unverified employers.
/// Throws <see cref="ConflictException"/> if the email is already registered and verified.
/// </summary>
public sealed class SendEmployerOtpCommandHandler : IRequestHandler<SendEmployerOtpCommand, bool>
{
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public SendEmployerOtpCommandHandler(
        IOtpService otpService,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _otpService = otpService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SendEmployerOtpCommand request, CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Employers.GetByEmailAsync(request.Email, cancellationToken);
        if (employer != null && employer.IsVerified)
        {
            throw new ConflictException($"An employer with email '{request.Email}' is already registered and verified. Please sign in.");
        }

        var recipientName = employer?.Name ?? "Employer";
        var otp = await _otpService.GenerateOtpAsync(request.Email, cancellationToken: cancellationToken);
        await _emailService.SendOtpEmailAsync(request.Email, recipientName, otp, cancellationToken);

        return true;
    }
}
