using AutoMapper;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Jobs.Queries.GetJob;

public class JobMappingProfile : Profile
{
    public JobMappingProfile()
    {
        CreateMap<Job, JobDto>();
    }
}
