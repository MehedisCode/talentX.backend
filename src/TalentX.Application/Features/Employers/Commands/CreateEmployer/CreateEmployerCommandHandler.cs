using System.Text.Json;
using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.DTOs.Auth;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Commands.CreateEmployer;

public sealed class CreateEmployerCommandHandler : IRequestHandler<CreateEmployerCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public CreateEmployerCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IOtpService otpService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        INotificationService notificationService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _otpService = otpService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    public async Task<AuthResponseDto> Handle(CreateEmployerCommand request, CancellationToken cancellationToken)
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

        var trimmedCompanyName = request.CompanyName.Trim();

        if (existingEmployer != null)
        {
            var existingCompanyWithName = await _unitOfWork.Companies.GetByNameAsync(trimmedCompanyName, cancellationToken);
            if (existingCompanyWithName != null && existingCompanyWithName.EmployerId != existingEmployer.UserId)
            {
                throw new ConflictException($"A company with the name '{trimmedCompanyName}' is already registered. Please use a different company name.");
            }

            existingEmployer.Update(request.Name);
            existingEmployer.UpdateCompany(trimmedCompanyName);
            existingEmployer.MarkAsVerified();

            var existingCompany = await _unitOfWork.Companies.GetByEmployerIdAsync(existingEmployer.UserId, cancellationToken);
            if (existingCompany is null)
            {
                var newCompany = new Company(existingEmployer.UserId, trimmedCompanyName);
                await _unitOfWork.Companies.AddAsync(newCompany, cancellationToken);
            }
            else
            {
                existingCompany.UpdateName(trimmedCompanyName);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _otpService.ConsumeEmailVerificationAsync(request.Email, cancellationToken);

            var existingTokenResult = await _tokenService.CreateAsync(existingEmployer.UserId);
            if (!existingTokenResult.Success || existingTokenResult.Data is null)
            {
                throw new ValidationException(string.Join("; ", existingTokenResult.Errors));
            }

            return new AuthResponseDto(
                Employer: _mapper.Map<EmployerDto>(existingEmployer),
                AccessToken: existingTokenResult.Data.AccessToken,
                RefreshToken: existingTokenResult.Data.RefreshToken
            );
        }

        var existingCompanyForRegistration = await _unitOfWork.Companies.GetByNameAsync(trimmedCompanyName, cancellationToken);
        if (existingCompanyForRegistration != null)
        {
            throw new ConflictException($"A company with the name '{trimmedCompanyName}' is already registered. Please use a different company name.");
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

        var createdUserId = identityResponse.Data.UserId;
        Employer employer;

        try
        {
            employer = new Employer(
                createdUserId,
                request.Name,
                request.Email,
                trimmedCompanyName
            );

            employer.MarkEmailVerified();
            await _unitOfWork.Employers.AddAsync(employer, cancellationToken);

            var company = new Company(employer.UserId, trimmedCompanyName);
            await _unitOfWork.Companies.AddAsync(company, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await PublishEmployerRegisteredNotificationsAsync(employer, cancellationToken);
        }
        catch (Exception)
        {
            await _identityService.DeleteApplicationUserAsync(createdUserId);
            throw;
        }

        await _otpService.ConsumeEmailVerificationAsync(request.Email, cancellationToken);

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

    private async Task PublishEmployerRegisteredNotificationsAsync(
        Employer employer,
        CancellationToken cancellationToken)
    {
        var adminUserIds = await _unitOfWork.Users.GetAdminUserIdsAsync(cancellationToken);

        var metadata = JsonSerializer.Serialize(new
        {
            employerId = employer.UserId,
            employerName = employer.Name,
            companyName = employer.CompanyName
        });

        foreach (var adminUserId in adminUserIds)
        {
            await _notificationService.PublishAsync(
                new PublishNotificationParameters(
                    adminUserId,
                    NotificationType.EmployerRegistered,
                    "New Employer Registration",
                    $"{employer.Name} from {employer.CompanyName} has registered and needs verification.",
                    metadata),
                cancellationToken);
        }
    }
}
