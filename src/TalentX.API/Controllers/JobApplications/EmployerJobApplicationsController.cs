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
    /// Gets all job applications for a specific job owned by the current employer.
    /// </summary>
    [HttpGet("{jobId:guid}/applications")]
    public async Task<IActionResult> GetJobApplicants(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetJobApplicantsQuery(jobId), cancellationToken);
        return Ok(result);
    }
}
