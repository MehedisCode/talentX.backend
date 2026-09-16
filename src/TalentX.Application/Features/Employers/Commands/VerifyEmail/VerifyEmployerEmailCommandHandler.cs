using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Auth;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

public sealed class VerifyEmployerEmailCommandHandler : IRequestHandler<VerifyEmployerEmailCommand, AuthResponseDto>
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

    public async Task<AuthResponseDto> Handle(VerifyEmployerEmailCommand request, CancellationToken cancellationToken)
    {
        var isValid = await _otpService.VerifyOtpAsync(request.Email, request.Otp, cancellationToken);
        if (!isValid)
        {
            throw new ValidationException("Invalid or expired OTP code.");
        }

        await _otpService.MarkEmailVerifiedAsync(request.Email, cancellationToken: cancellationToken);

        var employer = await _unitOfWork.Employers.GetByEmailAsync(request.Email, cancellationToken);
        if (employer == null)
        {
            return new AuthResponseDto(
                Employer: null,
                AccessToken: string.Empty,
                RefreshToken: string.Empty
            );
        }

        employer.MarkEmailVerified();
        var auditLog = new AuditLog(request.Email, "Login Success · OTP", isSystem: false);
        await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tokenResult = await _tokenService.CreateAsync(employer.UserId);
        if (!tokenResult.Success || tokenResult.Data is null)
        {
            throw new ValidationException(string.Join("; ", tokenResult.Errors));
        }

        return new AuthResponseDto(
            Employer: _mapper.Map<EmployerDto>(employer),
            AccessToken: tokenResult.Data.AccessToken,
            RefreshToken: tokenResult.Data.RefreshToken
        );
    }
}
