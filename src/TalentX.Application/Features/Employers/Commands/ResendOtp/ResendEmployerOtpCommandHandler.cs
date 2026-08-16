using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Employers.Commands.ResendOtp;

/// <summary>
/// Handler for <see cref="ResendEmployerOtpCommand"/> to dispatch a new verification OTP email.
/// </summary>
public sealed class ResendEmployerOtpCommandHandler : IRequestHandler<ResendEmployerOtpCommand, bool>
{
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public ResendEmployerOtpCommandHandler(
        IOtpService otpService,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _otpService = otpService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ResendEmployerOtpCommand request, CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Employers.GetByEmailAsync(request.Email, cancellationToken);
        if (employer == null)
        {
            throw new NotFoundException($"Employer with email '{request.Email}' not found.");
        }

        if (employer.IsVerified)
        {
            throw new ValidationException("This account is already verified. Please sign in.");
        }

        var otp = await _otpService.GenerateOtpAsync(request.Email, cancellationToken: cancellationToken);
        await _emailService.SendOtpEmailAsync(request.Email, employer.Name, otp, cancellationToken);

        return true;
    }
}
