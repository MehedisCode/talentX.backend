using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.JobApplications;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = nameof(UserRole.Employer))]
public sealed class EmployerJobApplicationsController : BaseApiController
{
    /// <summary>
    /// Gets job applications for a specific job owned by the current employer with pagination, search, and status filter.
    /// </summary>
    [HttpGet("{jobId:guid}/applications")]
    public async Task<IActionResult> GetJobApplicants(
        Guid jobId,
        [FromQuery] string? search,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetJobApplicantsQuery(jobId, search, status, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
