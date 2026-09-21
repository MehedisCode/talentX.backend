using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public string RecipientUserId { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Metadata { get; private set; }
    public DateTime? ReadAt { get; private set; }

    private Notification()
    {
    }

    public static Notification Create(
        string recipientUserId,
        NotificationType type,
        string title,
        string body,
        string? metadata)
    {
        return new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Body = body,
            Metadata = metadata
        };
    }

    public void MarkAsRead()
    {
        if (ReadAt is not null)
        {
            return;
        }

        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
