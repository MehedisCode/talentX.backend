namespace TalentX.Application.Common.Models;

public sealed record PresignedUploadTarget(
    string UploadUrl,
    string StorageKey,
    DateTimeOffset ExpiresAt,
    string HttpMethod = "PUT");
