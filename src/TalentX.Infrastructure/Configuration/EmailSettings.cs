namespace TalentX.Infrastructure.Configuration;

/// <summary>
/// Configuration options for outgoing SMTP email delivery.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderName { get; set; } = "TalentX Support";
    public string SenderEmail { get; set; } = "noreply@talentx.com";
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPass { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
