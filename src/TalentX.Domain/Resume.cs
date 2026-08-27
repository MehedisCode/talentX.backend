using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class Resume : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Resume() { }

    public Resume(
        string userId,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes)
    {
        UserId = userId;
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateFile(
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes)
    {
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
