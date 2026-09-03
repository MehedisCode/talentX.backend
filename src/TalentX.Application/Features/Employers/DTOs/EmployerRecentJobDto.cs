namespace TalentX.Application.Features.Employers.DTOs;

public sealed record EmployerRecentJobDto(
    Guid Id,
    string Title,
    string Status,
    DateTime CreatedAt,
    DateTime ExpireAt,
    int Vacancies,
    int ApplicantCount);
