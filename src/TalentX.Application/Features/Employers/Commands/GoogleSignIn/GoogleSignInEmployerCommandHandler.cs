using AutoMapper;
using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Employers;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Commands.GoogleSignIn;

/// <summary>
/// Handler for <see cref="GoogleSignInEmployerCommand"/> to verify Google ID token,
/// provision or find the employer account, ensure verified status, and return the employer profile DTO.
/// </summary>
public sealed class GoogleSignInEmployerCommandHandler : IRequestHandler<GoogleSignInEmployerCommand, EmployerDto>
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IExternalIdentityService _externalIdentityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GoogleSignInEmployerCommandHandler(
        IGoogleAuthService googleAuthService,
        IExternalIdentityService externalIdentityService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _googleAuthService = googleAuthService;
        _externalIdentityService = externalIdentityService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<EmployerDto> Handle(GoogleSignInEmployerCommand request, CancellationToken cancellationToken)
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

            return _mapper.Map<EmployerDto>(existingEmployer);
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

        return _mapper.Map<EmployerDto>(employer);
    }
}
