namespace TalentX.Application.Features.Talents.DTOs;

public sealed record TalentInvitationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string Company,
    DateTime InvitedDate);
