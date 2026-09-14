namespace TalentX.Application.Features.Companies.DTOs;

public sealed record PublicCompanyJobDto(
    Guid Id,
    string Title,
    string Location,
    string? City,
    string? Country,
    EmploymentType EmploymentType,
    WorkArrangement WorkArrangement,
    ExperienceLevel ExperienceLevel,
    int Vacancies,
    DateTime ExpireAt,
    DateTime? PublishedAt
);
