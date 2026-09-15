using AutoMapper;
using TalentX.Application.Features.JobAlerts.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Mappings;

public sealed class JobAlertMappingProfile : Profile
{
    public JobAlertMappingProfile()
    {
        CreateMap<JobAlert, JobAlertDto>();
    }
}
