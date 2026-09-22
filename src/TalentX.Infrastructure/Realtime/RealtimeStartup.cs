using Microsoft.Extensions.DependencyInjection;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Notifications.Services;

namespace TalentX.Infrastructure.Realtime;

public static class RealtimeStartup
{
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        services.AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
