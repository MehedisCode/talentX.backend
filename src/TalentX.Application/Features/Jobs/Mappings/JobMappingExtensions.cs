using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Jobs.Mappings;

public static class JobMappingExtensions
{
    public static JobListItemDto ToListItemDto(this Job job) => new()
    {
        Id = job.Id,
        Title = job.Title,
        CompanyName = job.CompanyName,
        Location = job.Location,
        EmploymentType = job.EmploymentType.ToString()
    };

    public static JobDetailsDto ToDetailsDto(this Job job) => new()
    {
        Id = job.Id,
        Title = job.Title,
        CompanyName = job.CompanyName,
        Description = job.Description,
        Location = job.Location,
        EmploymentType = job.EmploymentType.ToString(),
        ExperienceLevel = job.ExperienceLevel?.ToString(),
        WorkArrangement = job.WorkArrangement.ToString(),
        RequiredSkills = job.RequiredSkills.ToList(),
        Vacancies = job.Vacancies,
        CreatedAt = job.CreatedAt,
        ExpireAt = job.ExpireAt
    };
}
