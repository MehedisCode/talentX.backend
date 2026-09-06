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
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveJob(Guid jobId, CancellationToken cancellationToken)
    {
        var savedJobId = await Sender.Send(new SaveJobCommand(jobId), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, savedJobId);
    }

    [HttpDelete("{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSavedJob(Guid jobId, CancellationToken cancellationToken)
    {
        await Sender.Send(new RemoveSavedJobCommand(jobId), cancellationToken);
        return NoContent();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<SavedJobResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSavedJobs(
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetSavedJobsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("ids")]
    [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSavedJobIds(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetSavedJobIdsQuery(), cancellationToken);
        return Ok(result);
    }
}
