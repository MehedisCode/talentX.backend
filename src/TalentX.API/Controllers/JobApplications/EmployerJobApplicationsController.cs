using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobApplications.Commands.ScoreJobApplicants;
using TalentX.Application.Features.JobApplications.Queries.GetJobApplicants;
using TalentX.Application.Features.JobApplications.Queries.GetJobPipelineSummary;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.JobApplications;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = nameof(UserRole.Employer))]
public sealed class EmployerJobApplicationsController : BaseApiController
{
    /// <summary>
    /// Gets job applications for a specific job owned by the current employer with pagination, search, status filter, and sorting.
    /// </summary>
    [HttpGet("{jobId:guid}/applications")]
    public async Task<IActionResult> GetJobApplicants(
        Guid jobId,
        [FromQuery] string? search,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetJobApplicantsQuery(jobId, search, status, sortBy, sortOrder, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the candidate pipeline summary for a specific job owned by the current employer.
    /// </summary>
    [HttpGet("{jobId:guid}/pipeline-summary")]
    public async Task<IActionResult> GetJobPipelineSummary(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetJobPipelineSummaryQuery(jobId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Triggers AI scoring for all applicants of a specific job using the same 7 criteria as talent matching.
    /// </summary>
    [HttpPost("{jobId:guid}/score-applicants")]
    public async Task<IActionResult> ScoreApplicants(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ScoreJobApplicantsCommand(jobId), cancellationToken);
        return Ok(result);
    }
}
