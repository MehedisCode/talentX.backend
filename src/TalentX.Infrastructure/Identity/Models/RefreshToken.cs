namespace TalentX.Infrastructure.Identity.Models;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ApplicationUserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
