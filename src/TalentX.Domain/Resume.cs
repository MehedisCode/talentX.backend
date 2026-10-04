using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class Resume : BaseEntity
{
    public const int MaxPerUser = 5;
    public const int MaxDisplayNameLength = 100;

    public string UserId { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Resume() { }

    public Resume(
        string userId,
        string storageKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        string? displayName = null,
        bool isDefault = false)
    {
        UserId = userId;
        StorageKey = storageKey;
        OriginalFileName = originalFileName;
        DisplayName = DeriveDisplayName(displayName, originalFileName);
        ContentType = contentType;
        SizeBytes = sizeBytes;
        IsDefault = isDefault;
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

    public void Rename(string displayName)
    {
        DisplayName = DeriveDisplayName(displayName, OriginalFileName);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string DeriveDisplayName(string? displayName, string originalFileName)
    {
        var candidate = string.IsNullOrWhiteSpace(displayName)
            ? Path.GetFileNameWithoutExtension(originalFileName)
            : displayName.Trim();

        if (string.IsNullOrWhiteSpace(candidate))
            return "Resume";

        return candidate.Length > MaxDisplayNameLength
            ? candidate[..MaxDisplayNameLength]
            : candidate;
    }
}
