using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Invitations.Commands.RespondToInvitation;
using TalentX.Application.Features.Invitations.Commands.RespondToInvitationByToken;
using TalentX.Application.Features.Invitations.Queries.GetInvitationByToken;
using TalentX.Application.Features.Invitations.Queries.GetMyInvitations;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Talents;

public sealed record RespondToInvitationRequestDto(bool Accept);

[ApiController]
[Authorize(Roles = $"{nameof(UserRole.Talent)},{nameof(UserRole.Guest)}")]
[Route("api/v1/talents/invitations")]
public sealed class TalentInvitationsController : BaseApiController
{
    /// <summary>
    /// Gets all invitations for the currently authenticated talent.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyInvitations(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyInvitationsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets invitation details by token (does not require authentication).
    /// </summary>
    [HttpGet("by-token/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetInvitationByToken(string token, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetInvitationByTokenQuery(token), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Responds to an invitation (accept or decline).
    /// </summary>
    [HttpPatch("{invitationId:guid}/respond")]
    public async Task<IActionResult> RespondToInvitation(
        Guid invitationId,
        [FromBody] RespondToInvitationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RespondToInvitationCommand(invitationId, request.Accept),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Responds to an invitation by token (does not require authentication).
    /// </summary>
    [HttpPatch("by-token/{token}/respond")]
    [AllowAnonymous]
    public async Task<IActionResult> RespondToInvitationByToken(
        string token,
        [FromBody] RespondToInvitationRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RespondToInvitationByTokenCommand(token, request.Accept),
            cancellationToken);
        return Ok(result);
    }
}
