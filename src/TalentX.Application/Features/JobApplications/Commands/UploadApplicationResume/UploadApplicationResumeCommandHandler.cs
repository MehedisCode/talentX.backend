using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.JobApplications.Commands.UploadApplicationResume;

public sealed class UploadApplicationResumeCommandHandler
    : IRequestHandler<UploadApplicationResumeCommand, UploadApplicationResumeResponse>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPresignedUrlService _presignedUrlService;
    private readonly ILogger<UploadApplicationResumeCommandHandler> _logger;

    public UploadApplicationResumeCommandHandler(
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        IPresignedUrlService presignedUrlService,
        ILogger<UploadApplicationResumeCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _presignedUrlService = presignedUrlService;
        _logger = logger;
    }

    public async Task<UploadApplicationResumeResponse> Handle(
        UploadApplicationResumeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to upload a resume.");

        var sanitizedFileName = TalentMediaStorageKeyGuard.SanitizeFileNameSegment(request.File.FileName);
        var storageKey = TalentMediaStorageKeyGuard.BuildStorageKey(
            "resumes",
            userId,
            $"{Guid.NewGuid()}_{sanitizedFileName}");

        try
        {
            await _fileStorageService.SaveAsync(request.File, storageKey, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save application resume {FileName} to storage", sanitizedFileName);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }

        var resumeDownloadUrl = await _presignedUrlService.CreateDownloadUrlAsync(
            storageKey,
            TimeSpan.FromHours(1),
            cancellationToken);

        return new UploadApplicationResumeResponse(storageKey, sanitizedFileName, resumeDownloadUrl);
    }
}
