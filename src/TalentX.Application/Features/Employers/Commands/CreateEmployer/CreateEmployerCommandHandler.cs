using AutoMapper;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.DTOs.Employers;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.Commands.CreateEmployer;

/// <summary>
/// Handler for <see cref="CreateEmployerCommand"/> to register user in Identity, persist employer profile, dispatch verification OTP, and return projected DTO.
/// </summary>
public sealed class CreateEmployerCommandHandler : IRequestHandler<CreateEmployerCommand, EmployerDto>
{
    private readonly IIdentityService _identityService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateEmployerCommandHandler(
        IIdentityService identityService,
        IOtpService otpService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _identityService = identityService;
        _otpService = otpService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<EmployerDto> Handle(CreateEmployerCommand request, CancellationToken cancellationToken)
    {
        var exists = await _unitOfWork.Employers.ExistsByEmailAsync(request.Email, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"An employer with email '{request.Email}' already exists.");
        }

        var (succeeded, userId, errors) = await _identityService.CreateUserAsync(
            request.Email,
            request.Password,
            UserRole.Employer.ToString(),
            cancellationToken
        );

        if (!succeeded)
        {
            throw new ValidationException(string.Join("; ", errors));
        }

        var employer = new Employer(
            userId,
            request.Name,
            request.Email,
            request.CompanyName
        );

        await _unitOfWork.Employers.AddAsync(employer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var otp = await _otpService.GenerateOtpAsync(request.Email, cancellationToken: cancellationToken);
        await _emailService.SendOtpEmailAsync(request.Email, request.Name, otp, cancellationToken);

        return _mapper.Map<EmployerDto>(employer);
    }
}
