using AutoMapper;
using TalentX.Application.Features.Companies.DTOs;
using TalentX.Domain;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Mappings;

public sealed class CompanyMappingProfile : Profile
{
    public CompanyMappingProfile()
    {
        CreateMap<Job, PublicCompanyJobDto>();
    }
}
