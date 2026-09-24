using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;
using TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparation;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Talents;

[ApiController]
[Route("api/v1/talents/me/interviews")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class InterviewPreparationController : BaseApiController
{
    [HttpGet("preparations")]
    public async Task<IActionResult> GetMyPreparations(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPreparationsByTalentQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{invitationId:guid}/preparation")]
    public async Task<IActionResult> GetPreparation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPreparationQuery(invitationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{invitationId:guid}/preparation")]
    [EnableRateLimiting("InterviewPrepGenerate")]
    public async Task<IActionResult> GeneratePreparation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GeneratePreparationCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{invitationId:guid}/preparation/regenerate")]
    [EnableRateLimiting("InterviewPrepGenerate")]
    public async Task<IActionResult> RegeneratePreparation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RegeneratePreparationCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{invitationId:guid}/preparation/additional-questions")]
    [EnableRateLimiting("InterviewPrepGenerate")]
    public async Task<IActionResult> GenerateMoreQuestions(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GenerateAdditionalQuestionsCommand(invitationId), cancellationToken);
        return Ok(result);
    }
}
