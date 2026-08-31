using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;

public sealed class RequestMediaMultipartPartUrlCommandHandler
    : IRequestHandler<RequestMediaMultipartPartUrlCommand, string>
{
    private static readonly TimeSpan PartUploadUrlExpiry = TimeSpan.FromMinutes(15);

    private readonly IMultipartStorageService _multipartStorageService;
    private readonly ICurrentUserService _currentUserService;

    public RequestMediaMultipartPartUrlCommandHandler(
        IMultipartStorageService multipartStorageService,
        ICurrentUserService currentUserService)
    {
        _multipartStorageService = multipartStorageService;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(
        RequestMediaMultipartPartUrlCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to upload a video introduction.");

        TalentMediaStorageKeyGuard.EnsureOwnedByUser(request.StorageKey, userId, "video-intros");

        return await _multipartStorageService.CreatePartUploadUrlAsync(
            request.StorageKey,
            request.UploadId,
            request.PartNumber,
            PartUploadUrlExpiry,
            cancellationToken);
    }
}
