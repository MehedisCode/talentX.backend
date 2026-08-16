using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

/// <summary>
/// Handler for <see cref="VerifyEmployerEmailCommand"/> to verify email OTP and mark employer account as verified.
/// Completely independent of Identity services.
/// </summary>
public sealed class VerifyEmployerEmailCommandHandler : IRequestHandler<VerifyEmployerEmailCommand, bool>
{
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmployerEmailCommandHandler(
        IOtpService otpService,
        IUnitOfWork unitOfWork)
    {
        _otpService = otpService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(VerifyEmployerEmailCommand request, CancellationToken cancellationToken)
    {
        var employer = await _unitOfWork.Employers.GetByEmailAsync(request.Email, cancellationToken);
        if (employer == null)
        {
            throw new NotFoundException($"Employer with email '{request.Email}' not found.");
        }

        var isValid = await _otpService.VerifyOtpAsync(request.Email, request.Otp, cancellationToken);
        if (!isValid)
        {
            throw new ValidationException("Invalid or expired OTP code.");
        }

        employer.MarkAsVerified();
        _unitOfWork.Employers.Update(employer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
