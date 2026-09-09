using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class EmailDispatchLog : BaseEntity
{
    public string RecipientEmail { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string EmailType { get; private set; } = string.Empty;

    public EmailDispatchStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }

    public DateTime NextAttemptAt { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private EmailDispatchLog() { }

    public EmailDispatchLog(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        string emailType,
        int maxAttempts = 5)
    {
        RecipientEmail = recipientEmail;
        RecipientName = recipientName;
        Subject = subject;
        HtmlBody = htmlBody;
        EmailType = emailType;
        Status = EmailDispatchStatus.Pending;
        AttemptCount = 0;
        MaxAttempts = maxAttempts;
        NextAttemptAt = DateTime.UtcNow;
    }

    public void MarkProcessing()
    {
        Status = EmailDispatchStatus.Processing;
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSent()
    {
        Status = EmailDispatchStatus.Sent;
        ErrorMessage = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage, TimeSpan retryDelay)
    {
        ErrorMessage = errorMessage;
        LastAttemptAt = DateTime.UtcNow;

        if (AttemptCount >= MaxAttempts)
        {
            Status = EmailDispatchStatus.MaxRetriesExceeded;
        }
        else
        {
            Status = EmailDispatchStatus.Failed;
            NextAttemptAt = DateTime.UtcNow.Add(retryDelay);
        }

        UpdatedAt = DateTime.UtcNow;
    }
}
