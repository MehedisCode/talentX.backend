using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Commands.GoogleSignIn;

/// <summary>
/// Handler for <see cref="GoogleSignInEmployerCommand"/> to verify Google ID token,
/// provision or find the employer account, issue JWT session tokens, and return the composite authenticated DTO.
/// </summary>
public sealed class GoogleSignInEmployerCommandHandler : IRequestHandler<GoogleSignInEmployerCommand, AuthEmployerDto>
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IExternalIdentityService _externalIdentityService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GoogleSignInEmployerCommandHandler(
        IGoogleAuthService googleAuthService,
        IExternalIdentityService externalIdentityService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _googleAuthService = googleAuthService;
        _externalIdentityService = externalIdentityService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AuthEmployerDto> Handle(GoogleSignInEmployerCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Google ID token via public key cryptography
        var googleUser = await _googleAuthService.VerifyIdTokenAsync(request.IdToken, cancellationToken);

        // 2. Check if employer already exists in domain storage
        var existingEmployer = await _unitOfWork.Employers.GetByEmailAsync(googleUser.Email, cancellationToken);
        if (existingEmployer != null)
        {
            if (!existingEmployer.IsVerified)
            {
                existingEmployer.MarkAsVerified();
                _unitOfWork.Employers.Update(existingEmployer);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

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

        // 3. New employer: provision passwordless Identity account
        var (_, userId) = await _externalIdentityService.FindOrCreateExternalUserAsync(
            googleUser.Email,
            UserRole.Employer.ToString(),
            cancellationToken
        );

        // 4. Create verified domain Employer record
        var employer = new Employer(
            userId,
            googleUser.Name,
            googleUser.Email,
            companyName: string.Empty
        );
        employer.MarkAsVerified();

        await _unitOfWork.Employers.AddAsync(employer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Mint JWT access and refresh tokens for the newly provisioned employer
        var tokenResult = await _tokenService.CreateAsync(userId);
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
