using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.GenerateJobDescription;
using TalentX.Application.Features.Jobs.Commands.GenerateSkills;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetMyJob;
using TalentX.Application.Features.Jobs.Queries.GetMyJobs;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

namespace TalentX.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : BaseApiController
{
    /// <summary>
    /// Creates a new job posting (draft or published).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<ActionResult<Guid>> CreateJob([FromBody] CreateJobCommand command)
    {
        var jobId = await Sender.Send(command);
        return StatusCode(StatusCodes.Status201Created, jobId);
    }

    /// <summary>
    /// Updates an existing job posting.
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobCommand command)
    {
        await Sender.Send(command with { Id = id });
        return NoContent();
    }

    /// <summary>
    /// Gets all published jobs with pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetPublishedJobsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets job details by ID (public).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await Sender.Send(new GetJobDetailsQuery(id), cancellationToken);
        return Ok(job);
    }
    
    /// <summary>
    /// Gets a specific job belonging to the current employer.
    /// </summary>
    [HttpGet("mine/{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetMyJob(Guid id, CancellationToken cancellationToken)
    {
        var job = await Sender.Send(new GetMyJobQuery(id), cancellationToken);
        return Ok(job);
    }

    /// <summary>
    /// Gets all jobs belonging to the current employer.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetMyJobs(CancellationToken cancellationToken)
    {
        var jobs = await Sender.Send(new GetMyJobsQuery(), cancellationToken);
        return Ok(jobs);
    }

    /// <summary>
    /// Generates a job description using AI based on basic job details.
    /// Used in the multi-step wizard to assist employers.
    /// </summary>
    [HttpPost("generate-description")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GenerateDescription(
        [FromBody] GenerateJobDescriptionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generates relevant skills for a job using AI.
    /// Used in the multi-step wizard to assist employers.
    /// </summary>
    [HttpPost("generate-skills")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GenerateSkills(
        [FromBody] GenerateSkillsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

