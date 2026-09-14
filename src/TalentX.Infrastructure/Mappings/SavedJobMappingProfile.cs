using AutoMapper;
using TalentX.Application.Features.SavedJobs.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Mappings;

public sealed class SavedJobMappingProfile : Profile
{
    public SavedJobMappingProfile()
    {
        CreateMap<Job, SavedJobResponse>()
            .ForMember(d => d.SavedJobId, o => o.Ignore())
            .ForMember(d => d.JobId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.JobTitle, o => o.MapFrom(s => s.Title))
            .ForMember(d => d.EmploymentType, o => o.MapFrom(s => s.EmploymentType.ToString()))
            .ForMember(d => d.PostedDate, o => o.MapFrom(s => s.PublishedAt ?? s.CreatedAt))
            .ForMember(d => d.IsAvailable, o => o.MapFrom(s =>
                !s.IsDeleted && s.Status == JobStatus.Published && s.ExpireAt.Date >= DateTime.UtcNow.Date));
    }
}
