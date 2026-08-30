using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;

public sealed class ListMediaMultipartPartsQueryHandler
    : IRequestHandler<ListMediaMultipartPartsQuery, IReadOnlyList<UploadedPartDto>>
{
    private readonly IMultipartStorageService _multipartStorageService;
    private readonly ICurrentUserService _currentUserService;

    public ListMediaMultipartPartsQueryHandler(
        IMultipartStorageService multipartStorageService,
        ICurrentUserService currentUserService)
    {
        _multipartStorageService = multipartStorageService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<UploadedPartDto>> Handle(
        ListMediaMultipartPartsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view an upload session.");

        TalentMediaStorageKeyGuard.EnsureOwnedByUser(request.StorageKey, userId, "video-intros");

        return await _multipartStorageService.ListUploadedPartsAsync(
            request.StorageKey,
            request.UploadId,
            cancellationToken);
    }
}
