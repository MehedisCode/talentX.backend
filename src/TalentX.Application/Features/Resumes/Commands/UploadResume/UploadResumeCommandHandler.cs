using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Resumes.Commands.UploadResume;

public sealed class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, ResumeExtractionResult>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IResumeExtractionService _resumeExtractionService;
    private readonly ILogger<UploadResumeCommandHandler> _logger;

    public UploadResumeCommandHandler(
        IFileStorageService fileStorageService,
        IResumeExtractionService resumeExtractionService,
        ILogger<UploadResumeCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _resumeExtractionService = resumeExtractionService;
        _logger = logger;
    }

    public async Task<ResumeExtractionResult> Handle(
        UploadResumeCommand request,
        CancellationToken cancellationToken)
    {
        var resumeUploadId = Guid.NewGuid();

        string storageFileName = $"{resumeUploadId}_{request.File.FileName}";
        string fileStorageKey;
        try
        {
            fileStorageKey = await _fileStorageService.SaveAsync(request.File, storageFileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save uploaded file {FileName} to storage", request.File.FileName);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }

        ResumeExtractionResult extractionResult;
        try
        {
            extractionResult = await _resumeExtractionService.ExtractAsync(request.File, cancellationToken);
        }
        catch (Exception ex)
        {
            await TryDeleteStoredFileAsync(fileStorageKey, request.File.FileName);

            if (ex is UnprocessableEntityException)
                throw;

            _logger.LogError(ex, "Failed to extract candidate info using AI for file {FileName}", request.File.FileName);
            throw new BadGatewayException("AI extraction service is temporarily unavailable.");
        }

        extractionResult.ResumeUploadId = resumeUploadId;
        extractionResult.FileStorageKey = fileStorageKey;

        return extractionResult;
    }

    private async Task TryDeleteStoredFileAsync(string storageKey, string originalFileName)
    {
        try
        {
            await _fileStorageService.DeleteAsync(storageKey);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogWarning(
                cleanupEx,
                "Failed to clean up stored file {StorageKey} for {FileName} after an extraction error",
                storageKey,
                originalFileName);
        }
    }
}
