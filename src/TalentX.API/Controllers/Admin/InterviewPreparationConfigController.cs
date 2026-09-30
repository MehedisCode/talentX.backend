using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.InterviewPreparation.Commands.UpdateInterviewPreparationConfig;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Queries.GetInterviewPreparationConfig;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/interview-preparation-config")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class InterviewPreparationConfigController : BaseApiController
{
    /// <summary>
    /// Gets the current interview preparation configuration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(InterviewPreparationConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new GetInterviewPreparationConfigQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the interview preparation configuration.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(InterviewPreparationConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateConfig(
        [FromBody] UpdateInterviewPreparationConfigCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
