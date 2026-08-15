using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TalentX.Api.Controllers;

/// <summary>
/// Provides health and diagnostic endpoints for the API service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private static readonly DateTimeOffset ProcessStartTime = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns the health status, server date/time (UTC), and server uptime.
    /// </summary>
    /// <returns>Health and diagnostic information.</returns>
    [HttpGet]
    public IActionResult Health()
    {
        var now = DateTimeOffset.UtcNow;
        var uptime = now - ProcessStartTime;

        return Ok(new
        {
            Status = "Healthy",
            ServerDate = now.ToString("yyyy-MM-dd"),
            ServerTimeUtc = now,
            StartTimeUtc = ProcessStartTime,
            Uptime = $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s",
            UptimeSeconds = Math.Round(uptime.TotalSeconds, 2)
        });
    }
}