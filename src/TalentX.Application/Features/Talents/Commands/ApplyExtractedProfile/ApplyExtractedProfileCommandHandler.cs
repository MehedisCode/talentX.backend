using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;

public sealed class ApplyExtractedProfileCommandHandler : IRequestHandler<ApplyExtractedProfileCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ApplyExtractedProfileCommandHandler> _logger;

    public ApplyExtractedProfileCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        ILogger<ApplyExtractedProfileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(ApplyExtractedProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to update your profile.");

        string? resumeStorageKey = null;
        string? sanitizedResumeFileName = null;

        if (request.ResumeFile is not null)
        {
            // The original filename must be sanitized to a bare basename BEFORE the
            // uniquifying GUID is prepended — BuildStorageKey's own sanitization strips
            // everything up to the LAST path separator in whatever string it is given,
            // so composing "{guid}_{rawFileName}" first and sanitizing only afterwards
            // would silently swallow the GUID prefix whenever the original filename
            // contains a '/' or '\'. Sanitizing here keeps that invariant safe; the
            // sanitize call inside BuildStorageKey is then a no-op pass-through for the
            // already-clean composite, not a second, divergent transformation.
            sanitizedResumeFileName = TalentMediaStorageKeyGuard.SanitizeFileNameSegment(request.ResumeFile.FileName);
            var storageFileName = TalentMediaStorageKeyGuard.BuildStorageKey(
                "resumes",
                userId,
                $"{Guid.NewGuid()}_{sanitizedResumeFileName}");

            try
            {
                resumeStorageKey = await _fileStorageService.SaveAsync(request.ResumeFile, storageFileName, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to save resume {FileName} to storage", sanitizedResumeFileName);
                throw new BadGatewayException("File storage is temporarily unavailable.");
            }
        }

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        talent.UpdateProfile(
            !string.IsNullOrWhiteSpace(request.ProfileData.FullName) ? request.ProfileData.FullName : talent.Name,
            request.ProfileData.Tagline ?? talent.Tagline,
            request.ProfileData.ShortBio ?? talent.ShortBio,
            request.ProfileData.Location ?? talent.Location,
            request.ProfileData.Phone ?? talent.Phone,
            request.ProfileData.LinkedInUrl ?? talent.LinkedInUrl,
            request.ProfileData.AdditionalUrls ?? talent.AdditionalUrls,
            request.ProfileData.TotalYearsExperience ?? talent.TotalYearsExperience,
            request.ProfileData.ExpectedSalary ?? talent.ExpectedSalary,
            request.ProfileData.SkillPropositions ?? talent.SkillPropositions
        );

        talent.UpdateSkills(
            request.ProfileData.TechnicalSkills ?? talent.TechnicalSkills,
            request.ProfileData.SoftSkills ?? talent.SoftSkills,
            request.ProfileData.ToolsAndPlatforms ?? talent.ToolsAndPlatforms
        );

        await _unitOfWork.WorkExperiences.DeleteByUserIdAsync(userId, cancellationToken);
        foreach (var workExperience in request.ProfileData.WorkExperiences ?? [])
        {
            await _unitOfWork.WorkExperiences.AddAsync(
                new WorkExperience(
                    userId,
                    workExperience.CompanyName,
                    workExperience.JobTitle,
                    workExperience.StartDate,
                    workExperience.EndDate,
                    workExperience.IsCurrent),
                cancellationToken);
        }

        await _unitOfWork.Educations.DeleteByUserIdAsync(userId, cancellationToken);
        foreach (var education in request.ProfileData.Educations ?? [])
        {
            await _unitOfWork.Educations.AddAsync(
                new Education(
                    userId,
                    education.Institution,
                    education.Degree,
                    education.StartDate,
                    education.EndDate),
                cancellationToken);
        }

        await _unitOfWork.Projects.DeleteByUserIdAsync(userId, cancellationToken);
        foreach (var project in request.ProfileData.Projects ?? [])
        {
            if (string.IsNullOrWhiteSpace(project.ProjectName))
            {
                continue;
            }

            await _unitOfWork.Projects.AddAsync(
                new Project(
                    userId,
                    project.ProjectName,
                    project.Role,
                    string.IsNullOrWhiteSpace(project.ProjectType) ? "Professional" : project.ProjectType,
                    project.StartDate,
                    project.EndDate,
                    project.IsOngoing,
                    project.Description,
                    project.TechnologiesUsed ?? [],
                    project.ProjectUrl),
                cancellationToken);
        }

        string? previousResumeStorageKey = null;
        if (request.ResumeFile is not null && resumeStorageKey is not null && sanitizedResumeFileName is not null)
        {
            var existingResume = await _unitOfWork.Resumes.GetByUserIdAsync(userId, cancellationToken);
            previousResumeStorageKey = existingResume?.StorageKey;

            if (existingResume is null)
            {
                await _unitOfWork.Resumes.AddAsync(
                    new Resume(
                        userId,
                        resumeStorageKey,
                        sanitizedResumeFileName,
                        request.ResumeFile.ContentType,
                        request.ResumeFile.Length,
                        isDefault: true),
                    cancellationToken);
            }
            else
            {
                existingResume.UpdateFile(
                    resumeStorageKey,
                    sanitizedResumeFileName,
                    request.ResumeFile.ContentType,
                    request.ResumeFile.Length);
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // The new resume was already saved to storage above (this handler now saves
            // before committing, reversed from the old order). If the commit itself fails,
            // that brand-new file would otherwise be orphaned — best-effort clean it up
            // before rethrowing, mirroring this codebase's existing cleanup-on-failure pattern.
            if (resumeStorageKey is not null && sanitizedResumeFileName is not null)
            {
                await TryDeleteStoredFileAsync(resumeStorageKey, sanitizedResumeFileName, cancellationToken);
            }
            throw;
        }

        if (request.ResumeFile is not null &&
            !string.IsNullOrWhiteSpace(previousResumeStorageKey) &&
            previousResumeStorageKey != resumeStorageKey)
        {
            await _fileStorageService.DeleteAsync(previousResumeStorageKey, cancellationToken);
        }
    }

    private async Task TryDeleteStoredFileAsync(string storageKey, string originalFileName, CancellationToken cancellationToken)
    {
        try
        {
            await _fileStorageService.DeleteAsync(storageKey, cancellationToken);
        }
        catch (Exception cleanupException)
        {
            _logger.LogWarning(
                cleanupException,
                "Failed to clean up stored file {StorageKey} for {FileName} after a save-changes error",
                storageKey,
                originalFileName);
        }
    }
}
