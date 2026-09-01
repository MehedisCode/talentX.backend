using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Security;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Auth.Commands.VerifyGuestEmail;

public sealed class VerifyGuestEmailCommandHandler : IRequestHandler<VerifyGuestEmailCommand, VerifyGuestEmailResponse>
{
    private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
    private readonly IJobApplicationRepository _jobApplicationRepository;
    private readonly IIdentityService _identityService;
    private readonly ITalentRepository _talentRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyGuestEmailCommandHandler> _logger;

    public VerifyGuestEmailCommandHandler(
        IEmailVerificationTokenRepository verificationTokenRepository,
        IJobApplicationRepository jobApplicationRepository,
        IIdentityService identityService,
        ITalentRepository talentRepository,
        ITokenService tokenService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<VerifyGuestEmailCommandHandler> logger)
    {
        _verificationTokenRepository = verificationTokenRepository;
        _jobApplicationRepository = jobApplicationRepository;
        _identityService = identityService;
        _talentRepository = talentRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VerifyGuestEmailResponse> Handle(VerifyGuestEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenEntity = await _verificationTokenRepository.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new NotFoundException("Invalid verification link.");

        if (tokenEntity.IsExpired)
            throw new ValidationException("This verification link has expired. Please apply again.");

        if (tokenEntity.IsConsumed)
            throw new ValidationException("This verification link has already been used.");

        var email = tokenEntity.ApplicantEmail;
        var application = await _jobApplicationRepository.GetByIdAsync(tokenEntity.JobApplicationId, cancellationToken)
            ?? throw new NotFoundException("Application not found.");

        var existingTalent = await _talentRepository.GetByEmailAsync(email, cancellationToken);

        string talentUserId;
        string temporaryPassword;

        if (existingTalent is not null)
        {
            talentUserId = existingTalent.UserId;
            temporaryPassword = TemporaryPasswordGenerator.Generate();
            var changeResult = await _identityService.ChangePasswordAsync(talentUserId, "!", temporaryPassword);
            if (!changeResult.Success)
            {
                _logger.LogWarning("Failed to reset password for existing talent {UserId}: {Errors}",
                    talentUserId, string.Join(", ", changeResult.Errors));
            }
        }
        else
        {
            temporaryPassword = TemporaryPasswordGenerator.Generate();
            var createResult = await _identityService.CreateApplicationUserAsync(email, temporaryPassword, UserRole.Talent);
            if (!createResult.Success || createResult.Data is null)
            {
                throw new ConflictException(
                    $"An account is already registered with email '{email}'. Please sign in instead.");
            }
            talentUserId = createResult.Data.UserId;
            await _talentRepository.AddAsync(new Talent(talentUserId, application.ApplicantName, email), cancellationToken);
        }

        application.LinkTalentUser(talentUserId);
        application.UpdateStatus(ApplicationStatus.Applied);

        var otherPending = await _jobApplicationRepository.GetPendingByEmailAsync(email, cancellationToken);
        foreach (var pending in otherPending)
        {
            if (pending.Id != application.Id)
            {
                pending.UpdateStatus(ApplicationStatus.Rejected);
            }
        }

        tokenEntity.MarkConsumed();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendCredentialsEmailAsync(email, application.ApplicantName, temporaryPassword, cancellationToken);

        var tokenResult = await _tokenService.CreateAsync(talentUserId);
        if (!tokenResult.Success || tokenResult.Data is null)
        {
            throw new ValidationException(string.Join("; ", tokenResult.Errors));
        }

        return new VerifyGuestEmailResponse(
            talentUserId,
            email,
            tokenResult.Data.AccessToken,
            tokenResult.Data.RefreshToken);
    }
}
