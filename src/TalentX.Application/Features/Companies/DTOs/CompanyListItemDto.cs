namespace TalentX.Application.Features.Companies.DTOs;

public sealed record CompanyListItemDto(
    Guid Id,
    string Name,
    string? LogoUrl,
    string? Description,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Location,
    string? LinkedIn,
    string? Twitter,
    string? Facebook,
    int OpeningsCount,
    int VacanciesCount
);
