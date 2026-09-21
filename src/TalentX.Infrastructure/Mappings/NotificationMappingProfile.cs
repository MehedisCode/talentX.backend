using AutoMapper;
using TalentX.Application.Features.Notifications.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Infrastructure.Mappings;

public sealed class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>();
    }
}
