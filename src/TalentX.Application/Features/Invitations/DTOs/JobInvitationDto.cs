namespace TalentX.Application.Features.Invitations.DTOs;

public sealed class JobInvitationDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string TalentUserId { get; set; } = string.Empty;
    public string TalentName { get; set; } = string.Empty;
    public DateTime InvitedDate { get; set; }
    public bool IsActive { get; set; }
}
