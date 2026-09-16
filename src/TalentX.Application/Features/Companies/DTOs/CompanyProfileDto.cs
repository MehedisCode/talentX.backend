namespace TalentX.Application.Features.Companies.DTOs;

public sealed record CompanyProfileDto(
    Guid Id,
    string EmployerId,
    string Name,
    string? LogoUrl,
    string? Description,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Location,
    string? LinkedIn,
    string? Twitter,
    string? Facebook
);
