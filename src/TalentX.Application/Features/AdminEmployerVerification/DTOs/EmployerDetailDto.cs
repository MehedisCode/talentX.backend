using TalentX.Domain.Enums;

namespace TalentX.Application.Features.AdminEmployerVerification.DTOs;

public sealed record EmployerDetailDto(
    Guid Id,
    string UserId,
    string Name,
    string Email,
    string CompanyName,
    EmployerVerificationStatus VerificationStatus,
    string? RejectionReason,
    DateTime? VerifiedAt,
    DateTime? RejectedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
