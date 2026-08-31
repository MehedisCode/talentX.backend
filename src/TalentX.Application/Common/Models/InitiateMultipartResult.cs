namespace TalentX.Application.Common.Models;

public sealed record InitiateMultipartResult(
    string UploadId,
    string StorageKey,
    int PartSizeBytes);
