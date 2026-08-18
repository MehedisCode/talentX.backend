using AutoMapper;
using TalentX.Domain.Entities;

namespace TalentX.Application.Jobs.Queries.GetJob;

public class JobMappingProfile : Profile
{
    public JobMappingProfile()
    {
        CreateMap<Job, JobDto>();
    }
}
