using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Identity.Interfaces;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

/// <summary>
/// Handler for <see cref="VerifyEmployerEmailCommand"/> to verify email OTP.
/// If an employer record exists, marks it verified and mints JWT session tokens.
/// If not yet registered, verifies OTP and allows the user to proceed to onboarding.
/// </summary>
public sealed class VerifyEmployerEmailCommandHandler : IRequestHandler<VerifyEmployerEmailCommand, AuthEmployerDto>
{
    private readonly IOtpService _otpService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public VerifyEmployerEmailCommandHandler(
        IOtpService otpService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _otpService = otpService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AuthEmployerDto> Handle(VerifyEmployerEmailCommand request, CancellationToken cancellationToken)
    {
        var isValid = await _otpService.VerifyOtpAsync(request.Email, request.Otp, cancellationToken);
        if (!isValid)
        {
            throw new ValidationException("Invalid or expired OTP code.");
        }

        var employer = await _unitOfWork.Employers.GetByEmailAsync(request.Email, cancellationToken);
        if (employer == null)
        {
            // Valid OTP for a new email prior to profile onboarding completion
            return new AuthEmployerDto(
                Employer: null,
                AccessToken: string.Empty,
                RefreshToken: string.Empty
            );
        }

        employer.MarkAsVerified();
        _unitOfWork.Employers.Update(employer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mint JWT access and refresh tokens for automatic login upon verification
        var tokenResult = await _tokenService.CreateAsync(employer.UserId);
        if (!tokenResult.Success || tokenResult.Data is null)
        {
            throw new ValidationException(string.Join("; ", tokenResult.Errors));
        }

        return new AuthEmployerDto(
            Employer: _mapper.Map<EmployerDto>(employer),
            AccessToken: tokenResult.Data.AccessToken,
            RefreshToken: tokenResult.Data.RefreshToken
        );
    }
}
