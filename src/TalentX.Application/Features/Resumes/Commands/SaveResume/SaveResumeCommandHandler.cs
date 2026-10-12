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

        ResumeExtractionResult? extractedProfile = null;
        var aiServiceUnavailable = false;

        if (request.UpdateProfile)
        {
            try
            {
                var extractionResult = await _resumeExtractionService.ExtractAsync(
                    request.ResumeFile,
                    ResumeExtractionPurpose.CandidateProfile,
                    cancellationToken);
                extractedProfile = extractionResult;
                extractedProfile.ResumeUploadId = resume.Id;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "AI extraction service unavailable while saving resume {FileName}; resume saved without profile prefill",
                    sanitizedFileName);
                aiServiceUnavailable = true;
            }
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

        return SavedResumeDto.FromEntity(resume, extractedProfile, aiServiceUnavailable);
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
