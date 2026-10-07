using System.IO;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Security;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain;
using TalentX.Domain.Entities;
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
        var (tokenEntity, application) = await ValidateVerifyEmailRequestAsync(request.Token, cancellationToken);
        var email = tokenEntity.ApplicantEmail;

        var (talentUserId, temporaryPassword, isNewTalent) = await GetOrCreateTalentUserAsync(
            email, application.ApplicantName, application, cancellationToken);

        await LinkApplicationsToTalentAsync(email, talentUserId, application, cancellationToken);

        tokenEntity.MarkConsumed();
        var auditLog = new AuditLog(email, "Login Success · Email Verification", isSystem: false);
        await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (isNewTalent && temporaryPassword is not null)
        {
            await _emailService.SendCredentialsEmailAsync(email, application.ApplicantName, temporaryPassword, cancellationToken);
        }

        var (accessToken, refreshToken) = await GenerateAuthTokensAsync(talentUserId);

        return new VerifyGuestEmailResponse(
            talentUserId,
            email,
            accessToken,
            refreshToken,
            RequiresPasswordChange: isNewTalent);
    }

    private async Task<(EmailVerificationToken TokenEntity, JobApplication Application)> ValidateVerifyEmailRequestAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var tokenEntity = await _verificationTokenRepository.GetByTokenAsync(token, cancellationToken)
            ?? throw new NotFoundException("Invalid verification link.");

        if (tokenEntity.IsExpired)
            throw new ValidationException("This verification link has expired. Please apply again.");

        if (tokenEntity.IsConsumed)
            throw new ValidationException("This verification link has already been used.");

        var application = await _jobApplicationRepository.GetByIdAsync(tokenEntity.JobApplicationId, cancellationToken)
            ?? throw new NotFoundException("Application not found.");

        return (tokenEntity, application);
    }

    private async Task<(string UserId, string? TemporaryPassword, bool IsNewTalent)> GetOrCreateTalentUserAsync(
        string email,
        string applicantName,
        JobApplication application,
        CancellationToken cancellationToken)
    {
        var existingTalent = await _talentRepository.GetByEmailAsync(email, cancellationToken);
        if (existingTalent is not null)
        {
            return (existingTalent.UserId, null, false);
        }

        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        var createResult = await _identityService.CreateApplicationUserAsync(email, temporaryPassword, UserRole.Guest);
        if (!createResult.Success || createResult.Data is null)
        {
            throw new ConflictException(
                $"An account is already registered with email '{email}'. Please sign in instead.");
        }

        var talentUserId = createResult.Data.UserId;

        var talent = new Talent(talentUserId, applicantName, email);
        talent.UpdateProfile(
            name: applicantName,
            tagline: null,
            shortBio: null,
            location: application.Location,
            phone: null,
            linkedInUrl: null,
            additionalUrls: null,
            totalYearsExperience: application.TotalYearsExperience,
            expectedSalary: application.ExpectedSalary,
            skillPropositions: null);

        var technicalSkills = application.GetExtractedTechnicalSkills();
        var softSkills = application.GetExtractedSoftSkills();
        var toolsAndPlatforms = application.GetExtractedToolsAndPlatforms();
        talent.UpdateSkills(technicalSkills, softSkills, toolsAndPlatforms);

        await _talentRepository.AddAsync(talent, cancellationToken);

        await CreateResumeForGuestAsync(talentUserId, application, cancellationToken);
        await CreateWorkExperienceForGuestAsync(talentUserId, application, cancellationToken);
        await CreateEducationForGuestAsync(talentUserId, application, cancellationToken);

        return (talentUserId, temporaryPassword, true);
    }

    private async Task CreateResumeForGuestAsync(
        string talentUserId,
        JobApplication application,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(application.ResumeStorageKey))
            return;

        var originalFileName = TalentMediaStorageKeyGuard.ExtractOriginalFileName(application.ResumeStorageKey);
        if (string.IsNullOrWhiteSpace(originalFileName))
            return;

        var existingResumes = await _unitOfWork.Resumes.GetAllByUserIdAsync(talentUserId, cancellationToken);
        if (existingResumes.Count >= Resume.MaxPerUser)
            return;

        if (existingResumes.Any(resume => resume.StorageKey == application.ResumeStorageKey))
            return;

        await _unitOfWork.Resumes.AddAsync(
            new Resume(
                talentUserId,
                application.ResumeStorageKey,
                originalFileName,
                "application/octet-stream",
                0,
                isDefault: existingResumes.Count == 0),
            cancellationToken);
    }

    private async Task CreateWorkExperienceForGuestAsync(
        string talentUserId,
        JobApplication application,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.WorkExperiences.GetByUserIdAsync(talentUserId, cancellationToken);
        if (existing.Count > 0)
            return;

        var workExperiences = application.GetExtractedWorkExperience();
        foreach (var we in workExperiences)
        {
            if (string.IsNullOrWhiteSpace(we.CompanyName) && string.IsNullOrWhiteSpace(we.JobTitle))
                continue;

            await _unitOfWork.WorkExperiences.AddAsync(
                new WorkExperience(
                    talentUserId,
                    we.CompanyName ?? string.Empty,
                    we.JobTitle ?? string.Empty,
                    we.StartDate,
                    we.EndDate,
                    we.IsCurrent),
                cancellationToken);
        }
    }

    private async Task CreateEducationForGuestAsync(
        string talentUserId,
        JobApplication application,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.Educations.GetByUserIdAsync(talentUserId, cancellationToken);
        if (existing.Count > 0)
            return;

        var educations = application.GetExtractedEducation();
        foreach (var edu in educations)
        {
            if (string.IsNullOrWhiteSpace(edu.Institution) && string.IsNullOrWhiteSpace(edu.Degree))
                continue;

            await _unitOfWork.Educations.AddAsync(
                new Education(
                    talentUserId,
                    edu.Institution ?? string.Empty,
                    edu.Degree ?? string.Empty,
                    edu.StartDate,
                    edu.EndDate),
                cancellationToken);
        }
    }

    private async Task LinkApplicationsToTalentAsync(
        string email,
        string talentUserId,
        JobApplication application,
        CancellationToken cancellationToken)
    {
        application.LinkTalentUser(talentUserId);
        var previousStatus = application.Status;
        application.UpdateStatus(ApplicationStatus.Applied);
        await _unitOfWork.JobApplicationStatusHistories.AddAsync(
            JobApplicationStatusHistory.Create(application.Id, previousStatus, ApplicationStatus.Applied, talentUserId),
            cancellationToken);

        var talent = await _talentRepository.GetByUserIdAsync(talentUserId, cancellationToken);
        if (talent is not null)
        {
            var hasNullProfileFields =
                talent.Location is null ||
                talent.ExpectedSalary is null ||
                talent.TotalYearsExperience is null;

            if (hasNullProfileFields)
            {
                talent.UpdateProfile(
                    name: talent.Name,
                    tagline: talent.Tagline,
                    shortBio: talent.ShortBio,
                    location: talent.Location ?? application.Location,
                    phone: talent.Phone,
                    linkedInUrl: talent.LinkedInUrl,
                    additionalUrls: talent.AdditionalUrls,
                    totalYearsExperience: talent.TotalYearsExperience ?? application.TotalYearsExperience,
                    expectedSalary: talent.ExpectedSalary ?? application.ExpectedSalary,
                    skillPropositions: talent.SkillPropositions);
                _talentRepository.Update(talent);
            }

            var hasEmptySkills =
                talent.TechnicalSkills.Count == 0 &&
                talent.SoftSkills.Count == 0 &&
                talent.ToolsAndPlatforms.Count == 0;

            if (hasEmptySkills)
            {
                var technicalSkills = application.GetExtractedTechnicalSkills();
                var softSkills = application.GetExtractedSoftSkills();
                var toolsAndPlatforms = application.GetExtractedToolsAndPlatforms();
                if (technicalSkills.Count > 0 || softSkills.Count > 0 || toolsAndPlatforms.Count > 0)
                {
                    talent.UpdateSkills(technicalSkills, softSkills, toolsAndPlatforms);
                    _talentRepository.Update(talent);
                }
            }

            await CreateResumeForGuestAsync(talentUserId, application, cancellationToken);
            await CreateWorkExperienceForGuestAsync(talentUserId, application, cancellationToken);
            await CreateEducationForGuestAsync(talentUserId, application, cancellationToken);
        }

        var otherPending = await _jobApplicationRepository.GetPendingByEmailAsync(email, cancellationToken);
        foreach (var pending in otherPending)
        {
            if (pending.Id != application.Id)
            {
                pending.LinkTalentUser(talentUserId);
                var pendingPreviousStatus = pending.Status;
                pending.UpdateStatus(ApplicationStatus.Applied);
                await _unitOfWork.JobApplicationStatusHistories.AddAsync(
                    JobApplicationStatusHistory.Create(pending.Id, pendingPreviousStatus, ApplicationStatus.Applied, talentUserId),
                    cancellationToken);
            }
        }
    }

    private async Task<(string AccessToken, string RefreshToken)> GenerateAuthTokensAsync(string talentUserId)
    {
        var tokenResult = await _tokenService.CreateAsync(talentUserId);
        if (!tokenResult.Success || tokenResult.Data is null)
        {
            throw new ValidationException(string.Join("; ", tokenResult.Errors));
        }

        return (tokenResult.Data.AccessToken, tokenResult.Data.RefreshToken);
    }
}
