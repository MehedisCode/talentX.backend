using AutoMapper;
using TalentX.Application.Features.Talents.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Mappings;

public sealed class TalentMappingProfile : Profile
{
    public TalentMappingProfile()
    {
        CreateMap<WorkExperience, WorkExperienceItemDto>();
        CreateMap<Education, EducationItemDto>();
    }
}
