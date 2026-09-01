using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class EmailVerificationToken : BaseEntity
{
    public string Token { get; private set; } = string.Empty;
    public string ApplicantEmail { get; private set; } = string.Empty;
    public Guid JobApplicationId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    private EmailVerificationToken() { }

    public EmailVerificationToken(
        string token,
        string applicantEmail,
        Guid jobApplicationId,
        TimeSpan expiry)
    {
        Token = token;
        ApplicantEmail = applicantEmail;
        JobApplicationId = jobApplicationId;
        ExpiresAt = DateTime.UtcNow.Add(expiry);
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsValid => !IsExpired && !IsConsumed;

    public void MarkConsumed()
    {
        ConsumedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
