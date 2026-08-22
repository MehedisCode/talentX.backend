using AutoMapper;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Mappings;

public sealed class JobMappingProfile : Profile
{
    public JobMappingProfile()
    {
        CreateMap<Job, JobListItemDto>();
        CreateMap<Job, JobDetailsDto>();
    }
}
