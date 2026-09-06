using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.JobApplications.Commands.Apply;
using TalentX.Application.Features.Jobs.Commands.CloseJob;
using TalentX.Application.Features.Jobs.Commands.CreateJob;
using TalentX.Application.Features.Jobs.Commands.DeleteJob;
using TalentX.Application.Features.Jobs.Commands.GenerateJobDescription;
using TalentX.Application.Features.Jobs.Commands.GenerateSkills;
using TalentX.Application.Features.Jobs.Commands.ReopenJob;
using TalentX.Application.Features.Jobs.Commands.UpdateJob;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Queries.GetJobDetails;
using TalentX.Application.Features.Jobs.Queries.GetMyJob;
using TalentX.Application.Features.Jobs.Queries.GetMyJobs;
using TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.FileStorage;

namespace TalentX.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class JobsController : BaseApiController
{
    /// <summary>
    /// Creates a new job posting (draft or published).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<ActionResult<Guid>> CreateJob([FromBody] CreateJobCommand command)
    {
        var jobId = await Sender.Send(command);
        return StatusCode(StatusCodes.Status201Created, jobId);
    }

    /// <summary>
    /// Updates an existing job posting.
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobCommand command)
    {
        await Sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetJobs(
        [FromQuery] GetJobsQueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = queryParams ?? new GetJobsQueryParams();
        var query = new GetPublishedJobsQuery(parameters);

        var result = await Sender.Send(query, cancellationToken);
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
    [HttpGet("mine/{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> GetMyJob(Guid id, CancellationToken cancellationToken)
    {
        var job = await Sender.Send(new GetMyJobQuery(id), cancellationToken);
        return Ok(job);
    }

    /// <summary>
    /// Gets all jobs belonging to the current employer.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
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
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
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
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> GenerateSkills(
        [FromBody] GenerateSkillsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/apply")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Apply(
        Guid id,
        [FromForm] ApplyToJobRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ApplyToJobCommand(
            id,
            request.ApplicantName,
            request.ApplicantEmail,
            new FormFileUploadedFile(request.ResumeFile),
            request.CoverLetter,
            request.ExpectedSalary,
            request.TotalYearsExperience,
            request.Location);

        var response = await Sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> CloseJob(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new CloseJobCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/reopen")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> ReopenJob(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new ReopenJobCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> DeleteJob(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteJobCommand(id), cancellationToken);
        return NoContent();
    }
}
