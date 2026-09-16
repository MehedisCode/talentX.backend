using TalentX.Domain.Enums;

namespace TalentX.Application.Features.AdminEmployerVerification.DTOs;

public sealed record PendingEmployerDto(
    Guid Id,
    string Name,
    string Email,
    string CompanyName,
    EmployerVerificationStatus VerificationStatus,
    string? RejectionReason,
    DateTime CreatedAt
);
