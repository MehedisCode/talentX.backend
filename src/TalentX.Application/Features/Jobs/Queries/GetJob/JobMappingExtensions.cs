using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Jobs.Queries.GetJob;

public static class JobMappingExtensions
{
    public static JobDto ToDto(this Job job)
    {
        return new JobDto
        {
            Id = job.Id,
            EmployerId = job.EmployerId,
            Title = job.Title,
            CompanyName = job.CompanyName,
            Description = job.Description,
            EmploymentType = job.EmploymentType,
            ExperienceLevel = job.ExperienceLevel,
            Location = job.Location,
            WorkArrangement = job.WorkArrangement,
            RequiredSkills = job.RequiredSkills,
            Status = job.Status,
            Vacancies = job.Vacancies,
            ExpireAt = job.ExpireAt,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt
        };
    }
}
