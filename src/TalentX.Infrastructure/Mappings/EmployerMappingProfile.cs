using AutoMapper;
using TalentX.Application.DTOs.Employers;
using TalentX.Domain;

namespace TalentX.Infrastructure.Mappings;

public sealed class EmployerMappingProfile : Profile
{
    public EmployerMappingProfile()
    {
        CreateMap<Employer, EmployerDto>();
    }
}
