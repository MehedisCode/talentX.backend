using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Talents.Queries.GetTalentApplications;
using TalentX.Application.Features.Talents.Queries.GetTalentInvitations;
using TalentX.Application.Features.Talents.Queries.GetTalentMatches;


namespace TalentX.API.Controllers.Talents;

/// <summary>
/// Provides the Talent Dashboard — authenticated talents can fetch their
/// applications, invitations, and job matches from a single surface.
/// </summary>
[ApiController]
[Route("api/v1/talent/dashboard")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class TalentDashboardController : BaseApiController
{
    /// <summary>
    /// Returns a paginated list of jobs the authenticated talent has applied to.
    /// Optionally filter by status: Applied | Viewed | Interview | Hired | Rejected.
    /// </summary>
    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetTalentApplicationsQuery(status, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated list of active job invitations sent to the authenticated talent.
    /// Only invitations for currently published, non-expired jobs are included.
    /// </summary>
    [HttpGet("invitations")]
    public async Task<IActionResult> GetInvitations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetTalentInvitationsQuery(page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated list of open jobs algorithmically matched to the authenticated talent.
    /// Results are sorted by match score descending. Returns an empty page when no matches exist.
    /// </summary>
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int minScore = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetTalentMatchesQuery(page, pageSize, minScore),
            cancellationToken);
        return Ok(result);
    }
}
