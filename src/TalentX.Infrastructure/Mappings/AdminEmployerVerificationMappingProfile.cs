using AutoMapper;
using TalentX.Application.Features.AdminEmployerVerification.DTOs;
using TalentX.Domain;

namespace TalentX.Infrastructure.Mappings;

public sealed class AdminEmployerVerificationMappingProfile : Profile
{
    public AdminEmployerVerificationMappingProfile()
    {
        CreateMap<Employer, PendingEmployerDto>()
            .ConstructUsing(e => new PendingEmployerDto(
                e.Id,
                e.Name,
                e.Email,
                e.CompanyName,
                e.VerificationStatus,
                e.RejectionReason,
                e.CreatedAt));

        CreateMap<Employer, EmployerDetailDto>()
            .ConstructUsing(e => new EmployerDetailDto(
                e.Id,
                e.UserId,
                e.Name,
                e.Email,
                e.CompanyName,
                e.VerificationStatus,
                e.RejectionReason,
                e.VerifiedAt,
                e.RejectedAt,
                e.CreatedAt,
                e.UpdatedAt));
    }
}
