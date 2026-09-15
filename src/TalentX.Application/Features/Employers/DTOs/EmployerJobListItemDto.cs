using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Employers.DTOs;

public sealed record EmployerJobListItemDto(
    Guid Id,
    string Title,
    string CompanyName,
    string Status,
    EmploymentType EmploymentType,
    WorkArrangement WorkArrangement,
    ExperienceLevel ExperienceLevel,
    string Location,
    int Vacancies,
    DateTime ExpireAt,
    DateTime CreatedAt,
    int ApplicantCount,
    SalaryType? SalaryType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    Currency? SalaryCurrency,
    SalaryPeriod? SalaryPeriod);
