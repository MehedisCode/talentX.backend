using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Resumes.DTOs;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Resumes.Commands.SaveResume;

public sealed class SaveResumeCommandHandler : IRequestHandler<SaveResumeCommand, SavedResumeDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IResumeExtractionService _resumeExtractionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SaveResumeCommandHandler> _logger;

    public SaveResumeCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IResumeExtractionService resumeExtractionService,
        ICurrentUserService currentUserService,
        ILogger<SaveResumeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _resumeExtractionService = resumeExtractionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SavedResumeDto> Handle(
        SaveResumeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var existingCount = await _unitOfWork.Resumes.CountByUserIdAsync(userId, cancellationToken);
        if (existingCount >= Resume.MaxPerUser)
        {
            throw new ConflictException(
                $"You can save up to {Resume.MaxPerUser} resumes. Delete one before adding another.");
        }

        ResumeExtractionResult? extractionResult = null;
        if (request.UpdateProfile)
        {
            extractionResult = await ExtractAsync(request, cancellationToken);
        }

        var sanitizedFileName = TalentMediaStorageKeyGuard.SanitizeFileNameSegment(request.ResumeFile.FileName);
        var storageFileName = TalentMediaStorageKeyGuard.BuildStorageKey(
            "resumes",
            userId,
            $"{Guid.NewGuid()}_{sanitizedFileName}");

        string storageKey;
        try
        {
            storageKey = await _fileStorageService.SaveAsync(request.ResumeFile, storageFileName, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save resume {FileName} to storage", sanitizedFileName);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }

        var resume = new Resume(
            userId,
            storageKey,
            sanitizedFileName,
            request.ResumeFile.ContentType,
            request.ResumeFile.Length,
            request.DisplayName,
            isDefault: existingCount == 0);

        await _unitOfWork.Resumes.AddAsync(resume, cancellationToken);

        if (extractionResult is not null)
        {
            await ApplyExtractionToProfileAsync(userId, extractionResult, cancellationToken);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            await TryDeleteStoredFileAsync(storageKey, sanitizedFileName, cancellationToken);
            throw;
        }

        return SavedResumeDto.FromEntity(resume);
    }

    private async Task<ResumeExtractionResult> ExtractAsync(
        SaveResumeCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _resumeExtractionService.ExtractAsync(request.ResumeFile, cancellationToken);
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to extract candidate info from resume {FileName}",
                request.ResumeFile.FileName);
            throw new BadGatewayException("AI extraction service is temporarily unavailable.");
        }
    }

    private async Task ApplyExtractionToProfileAsync(
        string userId,
        ResumeExtractionResult extractionResult,
        CancellationToken cancellationToken)
    {
        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        talent.UpdateProfile(
            string.IsNullOrWhiteSpace(talent.Name) && !string.IsNullOrWhiteSpace(extractionResult.FullName)
                ? extractionResult.FullName
                : talent.Name,
            talent.Tagline ?? extractionResult.Tagline,
            talent.ShortBio ?? extractionResult.ShortBio,
            talent.Location ?? extractionResult.Location,
            talent.Phone ?? extractionResult.Phone,
            talent.LinkedInUrl ?? extractionResult.LinkedInUrl,
            talent.AdditionalUrls.Count == 0 ? extractionResult.PortfolioUrls : talent.AdditionalUrls,
            talent.TotalYearsExperience ?? extractionResult.TotalYearsExperience,
            talent.ExpectedSalary ?? extractionResult.ExpectedSalary,
            talent.SkillPropositions.Count == 0 ? extractionResult.SkillPropositions : talent.SkillPropositions
        );

        talent.UpdateSkills(
            extractionResult.TechnicalSkills,
            extractionResult.SoftSkills,
            extractionResult.ToolsAndPlatforms);

        await _unitOfWork.WorkExperiences.DeleteByUserIdAsync(userId, cancellationToken);
        foreach (var workExperience in extractionResult.WorkExperience)
        {
            if (string.IsNullOrWhiteSpace(workExperience.CompanyName) &&
                string.IsNullOrWhiteSpace(workExperience.JobTitle))
            {
                continue;
            }

            await _unitOfWork.WorkExperiences.AddAsync(
                new WorkExperience(
                    userId,
                    workExperience.CompanyName ?? string.Empty,
                    workExperience.JobTitle ?? string.Empty,
                    workExperience.StartDate,
                    workExperience.EndDate,
                    workExperience.IsCurrent),
                cancellationToken);
        }

        await _unitOfWork.Educations.DeleteByUserIdAsync(userId, cancellationToken);
        foreach (var education in extractionResult.Education)
        {
            if (string.IsNullOrWhiteSpace(education.Institution) &&
                string.IsNullOrWhiteSpace(education.Degree))
            {
                continue;
            }

            await _unitOfWork.Educations.AddAsync(
                new Education(
                    userId,
                    education.Institution ?? string.Empty,
                    education.Degree ?? string.Empty,
                    education.StartDate,
                    education.EndDate),
                cancellationToken);
        }
    }

    private async Task TryDeleteStoredFileAsync(
        string storageKey,
        string originalFileName,
        CancellationToken cancellationToken)
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
