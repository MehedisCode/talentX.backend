using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Invitations.Commands.SendJobInvitation;
using TalentX.Application.Features.Invitations.DTOs;
using TalentX.Application.Features.Invitations.Queries.GetJobInvitations;
using TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;
using TalentX.Application.Features.TalentMatching.DTOs;
using TalentX.Application.Features.TalentMatching.Queries.GetCandidateSummary;
using TalentX.Application.Features.TalentMatching.Queries.GetMatchedCandidates;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Employers;

public sealed record SendInvitationRequestDto(string TalentUserId);

[ApiController]
[Authorize(Roles = $"{nameof(UserRole.Employer)}")]
[Route("api/v1/employers")]
public sealed class EmployerTalentMatchingController : BaseApiController
{
    /// <summary>
    /// Triggers AI candidate evaluation and scoring for a published job.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/match")]
    [ProducesResponseType(typeof(RunJobTalentMatchingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunMatching(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RunJobTalentMatchingCommand(jobId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves ranked candidate matches for an employer's job.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}/candidates")]
    [ProducesResponseType(typeof(List<MatchedCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatchedCandidates(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMatchedCandidatesQuery(jobId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sends a job application invitation to a recommended talent.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/invite")]
    [ProducesResponseType(typeof(SendJobInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendInvitation(
        Guid jobId,
        [FromBody] SendInvitationRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new SendJobInvitationCommand(jobId, request.TalentUserId);
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists all invitations issued for a specific job.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}/invitations")]
    [ProducesResponseType(typeof(List<JobInvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobInvitations(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetJobInvitationsQuery(jobId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a comprehensive profile summary of a candidate for employer review.
    /// </summary>
    [HttpGet("candidates/{id}/summary")]
    [ProducesResponseType(typeof(CandidateProfileSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidateSummary(string id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCandidateSummaryQuery(id), cancellationToken);
        return Ok(result);
    }
}
