namespace TalentX.Application.Features.Talents.DTOs;

public sealed record TalentInvitationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string Company,
    DateTime InvitedDate,
    DateTime ExpiresAt,
    string Message,
    WorkArrangement WorkArrangement,
    EmploymentType EmploymentType);
