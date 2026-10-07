using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.ExtractResume;

public sealed class ExtractResumeCommandHandler : IRequestHandler<ExtractResumeCommand, ResumeExtractionResult>
{
    private readonly IResumeExtractionService _resumeExtractionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ExtractResumeCommandHandler> _logger;

    public ExtractResumeCommandHandler(
        IResumeExtractionService resumeExtractionService,
        ICurrentUserService currentUserService,
        ILogger<ExtractResumeCommandHandler> logger)
    {
        _resumeExtractionService = resumeExtractionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ResumeExtractionResult> Handle(
        ExtractResumeCommand request,
        CancellationToken cancellationToken)
    {
        _ = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to extract your resume.");

        ResumeExtractionResult extractionResult;
        try
        {
            extractionResult = await _resumeExtractionService.ExtractAsync(
                request.File,
                ResumeExtractionPurpose.CandidateProfile,
                cancellationToken);
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to extract candidate info using AI for file {FileName}", request.File.FileName);
            throw new BadGatewayException("AI extraction service is temporarily unavailable.");
        }

        // The file is never written to storage during extraction (this is the whole point
        // of the redesign) — ResumeUploadId is populated only for the shared
        // ResumeExtractionPreview UI's cosmetic "Upload ID" display, and FileStorageKey is
        // deliberately left at its default since nothing downstream relies on it anymore.
        extractionResult.ResumeUploadId = Guid.NewGuid();

        return extractionResult;
    }
}
