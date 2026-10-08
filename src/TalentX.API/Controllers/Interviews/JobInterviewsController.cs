using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;
using TalentX.Application.Features.Interviews.Queries.GetEmployerJobInterviews;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Interviews;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = nameof(UserRole.Employer))]
public sealed class JobInterviewsController : BaseApiController
{
    /// <summary>
    /// Sends interview invitations to a batch of job applications. Each candidate is processed
    /// independently and reported in the response as either invited or skipped with a reason —
    /// an application not found for this job, or a candidate with no registered talent account,
    /// is always skipped; an already-Hired application is always skipped and never overridable;
    /// an already-Rejected application or one with an existing pending invitation is skipped
    /// unless <c>command.Force</c> is set, in which case the rejection is overridden (or the
    /// existing pending invitation is cancelled) and a new invitation is sent.
    /// </summary>
    /// <param name="jobId">The job to send invitations for.</param>
    /// <param name="command">
    /// The invitation details and target application ids. <c>Force</c> (default <c>false</c>)
    /// bypasses the Rejected-application and pending-invite skip rules for this request, but
    /// never the Hired skip rule.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The per-candidate invite/skip results and summary counts.</returns>
    [HttpPost("{jobId:guid}/interviews/send-batch")]
    [ProducesResponseType(typeof(SendBatchInterviewInvitationsResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendBatchInterviewInvitations(
        Guid jobId,
        [FromBody] SendBatchInterviewInvitationsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command with { JobId = jobId }, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("{jobId:guid}/interviews")]
    public async Task<IActionResult> GetEmployerJobInterviews(
        Guid jobId,
        [FromQuery] InterviewInvitationStatus? status = null,
        [FromQuery] InterviewType? type = null,
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetEmployerJobInterviewsQuery(jobId, status, type, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
