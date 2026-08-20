using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Employers;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Commands.CreateEmployer;

/// <summary>
/// Handler for <see cref="CreateEmployerCommand"/> to register user in Identity, persist employer profile,
/// issue JWT session tokens, and return authenticated profile DTO.
/// </summary>
public sealed class CreateEmployerCommandHandler : IRequestHandler<CreateEmployerCommand, AuthEmployerDto>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateEmployerCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AuthEmployerDto> Handle(CreateEmployerCommand request, CancellationToken cancellationToken)
    {
        var existingEmployer = await _unitOfWork.Employers.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmployer != null)
        {
            if (existingEmployer.IsVerified)
            {
                throw new ConflictException($"An employer with email '{request.Email}' is already registered and verified. Please sign in.");
            }

            // Update profile with submitted details and mark verified
            existingEmployer.Update(request.Name);
            existingEmployer.UpdateCompany(request.CompanyName);
            existingEmployer.MarkAsVerified();
            _unitOfWork.Employers.Update(existingEmployer);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var tokenRes = await _tokenService.CreateAsync(existingEmployer.UserId);
            if (!tokenRes.Success || tokenRes.Data is null)
            {
                throw new ValidationException(string.Join("; ", tokenRes.Errors));
            }

            return new AuthEmployerDto(
                Employer: _mapper.Map<EmployerDto>(existingEmployer),
                AccessToken: tokenRes.Data.AccessToken,
                RefreshToken: tokenRes.Data.RefreshToken
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

        // Mint JWT access and refresh tokens for immediate logged-in session
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
