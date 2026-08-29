using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;

public sealed class RequestProfileMediaUploadUrlCommandHandler
    : IRequestHandler<RequestProfileMediaUploadUrlCommand, PresignedUploadTarget>
{
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly IPresignedUrlService _presignedUrlService;
    private readonly ICurrentUserService _currentUserService;

    public RequestProfileMediaUploadUrlCommandHandler(
        IPresignedUrlService presignedUrlService,
        ICurrentUserService currentUserService)
    {
        _presignedUrlService = presignedUrlService;
        _currentUserService = currentUserService;
    }

    public async Task<PresignedUploadTarget> Handle(
        RequestProfileMediaUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to upload a profile picture.");

        var extension = ContentTypeToExtension(request.ContentType);
        var storageKey = TalentMediaStorageKeyGuard.BuildStorageKey(
            "profile-pictures",
            userId,
            $"{Guid.NewGuid()}{extension}");

        return await _presignedUrlService.CreateUploadUrlAsync(
            storageKey,
            request.ContentType,
            UploadUrlExpiry,
            cancellationToken);
    }

    private static string ContentTypeToExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => throw new BadRequestException("Unsupported content type for profile picture upload.")
    };
}
