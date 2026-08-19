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
        City = job.City,
        Country = job.Country,
        RoleType = job.RoleType?.ToString()
    };

    public static JobDetailsDto ToDetailsDto(this Job job) => new()
    {
        Id = job.Id,
        Title = job.Title,
        CompanyName = job.CompanyName,
        Description = job.Description,
        Responsibilities = job.Responsibilities,
        City = job.City,
        Country = job.Country,
        RoleType = job.RoleType?.ToString(),
        ExperienceLevel = job.ExperienceLevel?.ToString(),
        LocationPref = job.LocationPref?.ToString(),
        MinOverlapHours = job.MinOverlapHours,
        SkillRequirements = job.SkillRequirements.ToList(),
        CreatedAt = job.CreatedAt,
        ExpiresAt = job.ExpiresAt
    };
}
