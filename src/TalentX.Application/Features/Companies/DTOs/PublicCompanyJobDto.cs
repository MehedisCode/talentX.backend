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
    DateTime? PublishedAt,
    // True when the job was relisted after its first publish (PublishedAt is the repost date).
    bool IsReposted
);
