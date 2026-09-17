using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace TalentX.Infrastructure.Realtime;

public static class RealtimeEndpointExtensions
{
    // Mapped under the same /api/v1 prefix as REST controllers (not the API root) because
    // the frontend's single Next.js rewrite rule forwards /api/backend/:path* to
    // {API_URL}/:path*, where API_URL already includes /api/v1 — a root-level hub route
    // would 404 through that rewrite.
    private const string NotificationsHubRoute = "/api/v1/hubs/notifications";

    public static IEndpointRouteBuilder MapNotificationHub(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<NotificationsHub>(NotificationsHubRoute);

        return endpoints;
    }
}
