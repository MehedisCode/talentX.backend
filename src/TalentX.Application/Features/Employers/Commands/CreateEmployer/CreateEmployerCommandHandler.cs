using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Commands.CreateEmployer;

public sealed class CreateEmployerCommandHandler : IRequestHandler<CreateEmployerCommand, AuthEmployerDto>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateEmployerCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IOtpService otpService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _otpService = otpService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AuthEmployerDto> Handle(CreateEmployerCommand request, CancellationToken cancellationToken)
    {
        var existingEmployer = await _unitOfWork.Employers.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmployer != null && existingEmployer.IsVerified)
        {
            throw new ConflictException($"An employer with email '{request.Email}' is already registered and verified. Please sign in.");
        }

        var isEmailVerified = await _otpService.IsEmailVerifiedAsync(request.Email, cancellationToken);
        if (!isEmailVerified)
        {
            throw new ValidationException("Email verification is required or your verification session has expired. Please verify your email with OTP first.");
        }

        if (existingEmployer != null)
        {
            existingEmployer.Update(request.Name);
            existingEmployer.UpdateCompany(request.CompanyName);
            existingEmployer.MarkAsVerified();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _otpService.ConsumeEmailVerificationAsync(request.Email, cancellationToken);

            var existingTokenResult = await _tokenService.CreateAsync(existingEmployer.UserId);
            if (!existingTokenResult.Success || existingTokenResult.Data is null)
            {
                throw new ValidationException(string.Join("; ", existingTokenResult.Errors));
            }

            return new AuthEmployerDto(
                Employer: _mapper.Map<EmployerDto>(existingEmployer),
                AccessToken: existingTokenResult.Data.AccessToken,
                RefreshToken: existingTokenResult.Data.RefreshToken
            );
        }

        var identityResponse = await _identityService.CreateApplicationUserAsync(
            request.Email,
            request.Password,
            UserRole.Employer
        );

        if (!identityResponse.Success || identityResponse.Data is null)
        {
            throw new ValidationException(string.Join("; ", identityResponse.Errors));
        }

        var employer = new Employer(
            identityResponse.Data.UserId,
            request.Name,
            request.Email,
            request.CompanyName
        );

        employer.MarkAsVerified();
        await _unitOfWork.Employers.AddAsync(employer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _otpService.ConsumeEmailVerificationAsync(request.Email, cancellationToken);

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
