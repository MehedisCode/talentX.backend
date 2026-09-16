using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace TalentX.Infrastructure.Realtime;

public static class RealtimeEndpointExtensions
{
    private const string NotificationsHubRoute = "/hubs/notifications";

    public static IEndpointRouteBuilder MapNotificationHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationsHub>(NotificationsHubRoute);

        return endpoints;
    }
}
