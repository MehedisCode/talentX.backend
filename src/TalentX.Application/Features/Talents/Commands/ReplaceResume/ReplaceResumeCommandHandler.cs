using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.ReplaceResume;

public sealed class ReplaceResumeCommandHandler : IRequestHandler<ReplaceResumeCommand, ResumeExtractionResult>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IResumeExtractionService _resumeExtractionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReplaceResumeCommandHandler> _logger;

    public ReplaceResumeCommandHandler(
        IFileStorageService fileStorageService,
        IResumeExtractionService resumeExtractionService,
        ICurrentUserService currentUserService,
        ILogger<ReplaceResumeCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _resumeExtractionService = resumeExtractionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ResumeExtractionResult> Handle(
        ReplaceResumeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to replace your resume.");

        var resumeUploadId = Guid.NewGuid();
        var sanitizedUserId = Path.GetFileName(userId);
        var sanitizedFileName = Path.GetFileName(request.File.FileName);
        var storageFileName = $"resumes/{sanitizedUserId}/{resumeUploadId}_{sanitizedFileName}";

        string fileStorageKey;
        try
        {
            fileStorageKey = await _fileStorageService.SaveAsync(request.File, storageFileName, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save replacement resume {FileName} to storage", sanitizedFileName);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }

        ResumeExtractionResult extractionResult;
        try
        {
            extractionResult = await _resumeExtractionService.ExtractAsync(request.File, cancellationToken);
        }
        catch (Exception exception)
        {
            await TryDeleteStoredFileAsync(fileStorageKey, sanitizedFileName, cancellationToken);

            if (exception is UnprocessableEntityException)
                throw;

            _logger.LogError(exception, "Failed to extract candidate info using AI for file {FileName}", sanitizedFileName);
            throw new BadGatewayException("AI extraction service is temporarily unavailable.");
        }

        extractionResult.ResumeUploadId = resumeUploadId;
        extractionResult.FileStorageKey = fileStorageKey;

        return extractionResult;
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
                "Failed to clean up stored file {StorageKey} for {FileName} after an extraction error",
                storageKey,
                originalFileName);
        }
    }
}
