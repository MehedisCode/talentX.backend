using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Interviews.Commands.AcceptInterviewInvitation;
using TalentX.Application.Features.Interviews.Commands.DeclineInterviewInvitation;
using TalentX.Application.Features.Interviews.Queries.GetTalentInterviews;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Interviews;

[ApiController]
[Route("api/v1/talents")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class TalentInterviewsController : BaseApiController
{
    [HttpGet("me/interviews")]
    public async Task<IActionResult> GetMyInterviews(
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = PaginationRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetTalentInterviewsQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/interviews/{invitationId:guid}/accept")]
    public async Task<IActionResult> AcceptInterviewInvitation(
        Guid invitationId,
        [FromBody] AcceptInterviewInvitationCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command with { InvitationId = invitationId }, cancellationToken);
        return Ok(new { Message = "Interview invitation accepted successfully." });
    }

    [HttpPost("me/interviews/{invitationId:guid}/decline")]
    public async Task<IActionResult> DeclineInterviewInvitation(
        Guid invitationId,
        [FromBody] DeclineInterviewInvitationCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command with { InvitationId = invitationId }, cancellationToken);
        return Ok(new { Message = "Interview invitation declined." });
    }
}
