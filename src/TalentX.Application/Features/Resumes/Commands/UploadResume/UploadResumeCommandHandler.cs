using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Resumes.Commands.UploadResume;

public sealed class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, ResumeExtractionResult>
{
    private readonly IResumeExtractionService _resumeExtractionService;
    private readonly ILogger<UploadResumeCommandHandler> _logger;

    public UploadResumeCommandHandler(
        IResumeExtractionService resumeExtractionService,
        ILogger<UploadResumeCommandHandler> logger)
    {
        _resumeExtractionService = resumeExtractionService;
        _logger = logger;
    }

    public async Task<ResumeExtractionResult> Handle(
        UploadResumeCommand request,
        CancellationToken cancellationToken)
    {
        ResumeExtractionResult extractionResult;
        try
        {
            extractionResult = await _resumeExtractionService.ExtractAsync(request.File, cancellationToken);
        }
        catch (UnprocessableEntityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract candidate info using AI for file {FileName}", request.File.FileName);
            throw new BadGatewayException("AI extraction service is temporarily unavailable.");
        }

        extractionResult.ResumeUploadId = Guid.NewGuid();

        return extractionResult;
    }
}
