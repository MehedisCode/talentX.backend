namespace TalentX.Application.Features.Invitations.Commands.SendJobInvitation;

public sealed record SendJobInvitationResponse(
    Guid Id,
    Guid JobId,
    string TalentUserId,
    DateTime InvitedDate,
    bool IsActive);
