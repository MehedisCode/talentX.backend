using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.SavedJobs.Commands.RemoveSavedJob;
using TalentX.Application.Features.SavedJobs.Commands.SaveJob;
using TalentX.Application.Features.SavedJobs.DTOs;
using TalentX.Application.Features.SavedJobs.Queries.GetSavedJobIds;
using TalentX.Application.Features.SavedJobs.Queries.GetSavedJobs;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.SavedJobs;

[ApiController]
[Route("api/v1/saved-jobs")]
[Authorize(Roles = nameof(UserRole.Talent))]
public class SavedJobsController : BaseApiController
{
    [HttpPost("{jobId:guid}")]
    public async Task<IActionResult> SaveJob(Guid jobId, CancellationToken cancellationToken)
    {
        var savedJobId = await Sender.Send(new SaveJobCommand(jobId), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, savedJobId);
    }

    [HttpDelete("{jobId:guid}")]
    public async Task<IActionResult> RemoveSavedJob(Guid jobId, CancellationToken cancellationToken)
    {
        await Sender.Send(new RemoveSavedJobCommand(jobId), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetSavedJobs(
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetSavedJobsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("ids")]
    public async Task<IActionResult> GetSavedJobIds(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSavedJobIdsQuery(), cancellationToken);
        return Ok(result);
    }
}
