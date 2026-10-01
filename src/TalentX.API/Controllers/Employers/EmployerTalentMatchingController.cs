using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    /// Triggers AI candidate evaluation and scoring for an active or expired job.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/match")]
    [EnableRateLimiting("RunMatching")]
    public async Task<IActionResult> RunMatching(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RunJobTalentMatchingCommand(jobId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves ranked candidate matches for an employer's job.
    /// </summary>
    [HttpGet("jobs/{jobId:guid}/candidates")]
    [EnableRateLimiting("GetCandidates")]
    public async Task<IActionResult> GetMatchedCandidates(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMatchedCandidatesQuery(jobId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sends a job application invitation to a recommended talent.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/invite")]
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
    public async Task<IActionResult> GetJobInvitations(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetJobInvitationsQuery(jobId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Withdraws an active invitation sent for a job.
    /// </summary>
    [HttpPatch("jobs/{jobId:guid}/invitations/{invitationId:guid}/withdraw")]
    public async Task<IActionResult> WithdrawInvitation(
        Guid jobId,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new TalentX.Application.Features.Invitations.Commands.WithdrawInvitation.WithdrawInvitationCommand(jobId, invitationId),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a comprehensive profile summary of a candidate for employer review.
    /// </summary>
    [HttpGet("candidates/{id}/summary")]
    [EnableRateLimiting("CandidateSummary")]
    public async Task<IActionResult> GetCandidateSummary(string id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCandidateSummaryQuery(id), cancellationToken);
        return Ok(result);
    }
}
