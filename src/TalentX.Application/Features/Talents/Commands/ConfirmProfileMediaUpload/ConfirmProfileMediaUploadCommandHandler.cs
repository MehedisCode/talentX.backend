using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;

public sealed class ConfirmProfileMediaUploadCommandHandler : IRequestHandler<ConfirmProfileMediaUploadCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;

    public ConfirmProfileMediaUploadCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ConfirmProfileMediaUploadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to confirm a profile picture upload.");

        TalentMediaStorageKeyGuard.EnsureOwnedByUser(request.StorageKey, userId, "profile-pictures");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"Talent profile for user '{userId}' was not found.");

        var previousStorageKey = talent.ProfilePictureStorageKey;

        talent.UpdateProfilePicture(request.StorageKey);
        _unitOfWork.Talents.Update(talent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousStorageKey) && previousStorageKey != request.StorageKey)
        {
            await _fileStorageService.DeleteAsync(previousStorageKey, cancellationToken);
        }
    }
}
