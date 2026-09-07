using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Interviews.Commands.SendBatchInterviewInvitations;
using TalentX.Application.Features.Interviews.Queries.GetEmployerJobInterviews;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Interviews;

[ApiController]
[Route("api/v1/jobs")]
[Authorize(Roles = nameof(UserRole.Employer))]
public sealed class JobInterviewsController : BaseApiController
{
    [HttpPost("{jobId:guid}/interviews/send-batch")]
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
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetEmployerJobInterviewsQuery(jobId), cancellationToken);
        return Ok(result);
    }
}
