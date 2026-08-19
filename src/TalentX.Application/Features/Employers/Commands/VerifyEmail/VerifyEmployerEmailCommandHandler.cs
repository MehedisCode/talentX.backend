using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Identity.Interfaces;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

/// <summary>
/// Handler for <see cref="VerifyEmployerEmailCommand"/> to verify email OTP, mark employer account as verified,
/// and issue JWT session tokens for immediate automatic login.
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
