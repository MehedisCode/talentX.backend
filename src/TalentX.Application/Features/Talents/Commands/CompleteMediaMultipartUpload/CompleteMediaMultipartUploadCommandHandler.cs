using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;

public sealed class CompleteMediaMultipartUploadCommandHandler : IRequestHandler<CompleteMediaMultipartUploadCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMultipartStorageService _multipartStorageService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;

    public CompleteMediaMultipartUploadCommandHandler(
        IUnitOfWork unitOfWork,
        IMultipartStorageService multipartStorageService,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _multipartStorageService = multipartStorageService;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(CompleteMediaMultipartUploadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to upload a video introduction.");

        TalentMediaStorageKeyGuard.EnsureOwnedByUser(request.StorageKey, userId, "video-intros");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        var parts = request.Parts
            .Select(part => new PartETagDto(part.PartNumber, part.ETag))
            .ToList();

        await _multipartStorageService.CompleteMultipartUploadAsync(
            request.StorageKey,
            request.UploadId,
            parts,
            cancellationToken);

        var previousStorageKey = talent.VideoIntroStorageKey;

        talent.UpdateVideoIntro(request.StorageKey);
        _unitOfWork.Talents.Update(talent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousStorageKey) && previousStorageKey != request.StorageKey)
        {
            await _fileStorageService.DeleteAsync(previousStorageKey, cancellationToken);
        }
    }
}
