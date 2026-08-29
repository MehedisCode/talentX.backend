using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;

public sealed class InitiateMediaMultipartUploadCommandHandler
    : IRequestHandler<InitiateMediaMultipartUploadCommand, InitiateMultipartResult>
{
    private readonly IMultipartStorageService _multipartStorageService;
    private readonly ICurrentUserService _currentUserService;

    public InitiateMediaMultipartUploadCommandHandler(
        IMultipartStorageService multipartStorageService,
        ICurrentUserService currentUserService)
    {
        _multipartStorageService = multipartStorageService;
        _currentUserService = currentUserService;
    }

    public async Task<InitiateMultipartResult> Handle(
        InitiateMediaMultipartUploadCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to upload a video introduction.");

        var extension = ContentTypeToExtension(request.ContentType);
        var storageKey = TalentMediaStorageKeyGuard.BuildStorageKey(
            "video-intros",
            userId,
            $"{Guid.NewGuid()}{extension}");

        return await _multipartStorageService.InitiateMultipartUploadAsync(
            storageKey,
            request.ContentType,
            cancellationToken);
    }

    private static string ContentTypeToExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "video/mp4" => ".mp4",
        "video/webm" => ".webm",
        "video/quicktime" => ".mov",
        _ => throw new BadRequestException("Unsupported content type for video introduction upload.")
    };
}
