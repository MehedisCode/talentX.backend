using TalentX.Domain.Enums;

namespace TalentX.Application.DTOs.Employers;

/// <summary>
/// Response data transfer object representing an employer account.
/// </summary>
/// <param name="Id">Domain entity unique identifier.</param>
/// <param name="UserId">Associated Identity user identifier.</param>
/// <param name="Name">Full name of the employer.</param>
/// <param name="Email">Email address.</param>
/// <param name="CompanyName">Company or organization name.</param>
/// <param name="Role">User domain role.</param>
/// <param name="CreatedAt">Entity creation timestamp in UTC.</param>
/// <param name="UpdatedAt">Entity last update timestamp in UTC.</param>
public sealed record EmployerDto(
    Guid Id,
    string UserId,
    string Name,
    string Email,
    string CompanyName,
    UserRole Role,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
