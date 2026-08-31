using TalentX.Application.Common.Models;

namespace TalentX.Application.Common.Interfaces;

public interface IPresignedUrlService
{
    Task<PresignedUploadTarget> CreateUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<string> CreateDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);
}
