using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;

public sealed class AbortMediaMultipartUploadCommandHandler : IRequestHandler<AbortMediaMultipartUploadCommand>
{
    private readonly IMultipartStorageService _multipartStorageService;
    private readonly ICurrentUserService _currentUserService;

    public AbortMediaMultipartUploadCommandHandler(
        IMultipartStorageService multipartStorageService,
        ICurrentUserService currentUserService)
    {
        _multipartStorageService = multipartStorageService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AbortMediaMultipartUploadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to abort a video introduction upload.");

        TalentMediaStorageKeyGuard.EnsureOwnedByUser(request.StorageKey, userId, "video-intros");

        await _multipartStorageService.AbortMultipartUploadAsync(
            request.StorageKey,
            request.UploadId,
            cancellationToken);
    }
}
