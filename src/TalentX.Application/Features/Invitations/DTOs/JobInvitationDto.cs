using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Invitations.DTOs;

public sealed class JobInvitationDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string TalentUserId { get; set; } = string.Empty;
    public string TalentName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime InvitedDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; }
    public bool IsActive { get; set; }
}
