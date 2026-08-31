using AutoMapper;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Mappings;

public sealed class JobMappingProfile : Profile
{
    public JobMappingProfile()
    {
        CreateMap<Job, JobListItemDto>()
            .ForMember(d => d.EmploymentType, o => o.MapFrom(s => s.EmploymentType.ToString()))
            .ForMember(d => d.ExperienceLevel, o => o.MapFrom(s => s.ExperienceLevel.ToString()))
            .ForMember(d => d.WorkArrangement, o => o.MapFrom(s => s.WorkArrangement.ToString()));

        CreateMap<Job, JobDetailsDto>();
    }
}
