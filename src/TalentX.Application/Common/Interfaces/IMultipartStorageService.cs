using TalentX.Application.Common.Models;

namespace TalentX.Application.Common.Interfaces;

public interface IMultipartStorageService
{
    Task<InitiateMultipartResult> InitiateMultipartUploadAsync(
        string storageKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> CreatePartUploadUrlAsync(
        string storageKey,
        string uploadId,
        int partNumber,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UploadedPartDto>> ListUploadedPartsAsync(
        string storageKey,
        string uploadId,
        CancellationToken cancellationToken = default);

    Task CompleteMultipartUploadAsync(
        string storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> parts,
        CancellationToken cancellationToken = default);

    Task AbortMultipartUploadAsync(
        string storageKey,
        string uploadId,
        CancellationToken cancellationToken = default);
}
