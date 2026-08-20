using AutoMapper;
using TalentX.Application.DTOs.Employers;
using TalentX.Domain;

namespace TalentX.Infrastructure.Mappings;

/// <summary>
/// AutoMapper profile for Employer entity to DTO projections.
/// </summary>
public sealed class EmployerMappingProfile : Profile
{
    public EmployerMappingProfile()
    {
        CreateMap<Employer, EmployerDto>();
    }
}
