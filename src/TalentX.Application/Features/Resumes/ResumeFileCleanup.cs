using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Resumes;

/// <summary>
/// Deletes a stored resume object only once nothing references it anymore: it
/// must be absent from every talent's profile library and from every application
/// whose job is still live. Application rows for soft-deleted jobs are kept for
/// records but no longer keep the object alive.
/// </summary>
public static class ResumeFileCleanup
{
    public static async Task<bool> TryDeleteIfUnreferencedAsync(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return false;

        if (await unitOfWork.Resumes.ExistsByStorageKeyAsync(storageKey, cancellationToken))
            return false;

        var activeApplicationCount = await unitOfWork.JobApplications
            .CountActiveByResumeStorageKeyAsync(storageKey, cancellationToken);

        if (activeApplicationCount > 0)
            return false;

        await fileStorageService.DeleteAsync(storageKey, cancellationToken);
        return true;
    }
}
