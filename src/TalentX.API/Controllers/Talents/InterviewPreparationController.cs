using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;
using TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparation;
using TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;
using TalentX.Domain.Enums;

namespace TalentX.API.Controllers.Talents;

/// <summary>
/// Provides endpoints for generating, regenerating, and viewing AI interview
/// preparations for a talent's accepted interview invitations.
/// </summary>
[ApiController]
[Route("api/v1/talents/me/interviews")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class InterviewPreparationController : BaseApiController
{
    /// <summary>
    /// Gets all interview preparations belonging to the current talent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of interview preparations.</returns>
    [HttpGet("preparations")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewPreparationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyPreparations(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPreparationsByTalentQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the interview preparation for a specific interview invitation.
    /// </summary>
    /// <param name="invitationId">The unique ID of the interview invitation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The interview preparation details.</returns>
    [HttpGet("{invitationId:guid}/preparation")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [ProducesResponseType(typeof(InterviewPreparationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreparation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPreparationQuery(invitationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generates a new AI interview preparation for an accepted interview invitation.
    /// </summary>
    /// <param name="invitationId">The unique ID of the interview invitation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly generated interview preparation.</returns>
    [HttpPost("{invitationId:guid}/preparation")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [EnableRateLimiting("InterviewPrepGenerate")]
    [ProducesResponseType(typeof(InterviewPreparationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GeneratePreparation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GeneratePreparationCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Regenerates an existing interview preparation with fresh AI content.
    /// </summary>
    /// <param name="invitationId">The unique ID of the interview invitation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The regenerated interview preparation.</returns>
    [HttpPost("{invitationId:guid}/preparation/regenerate")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [EnableRateLimiting("InterviewPrepGenerate")]
    [ProducesResponseType(typeof(InterviewPreparationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> RegeneratePreparation(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RegeneratePreparationCommand(invitationId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Generates additional practice questions for an existing interview preparation.
    /// </summary>
    /// <param name="invitationId">The unique ID of the interview invitation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The interview preparation including the additional questions.</returns>
    [HttpPost("{invitationId:guid}/preparation/additional-questions")]
    [Authorize(Roles = nameof(UserRole.Talent))]
    [EnableRateLimiting("InterviewPrepGenerate")]
    [ProducesResponseType(typeof(InterviewPreparationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GenerateMoreQuestions(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GenerateAdditionalQuestionsCommand(invitationId), cancellationToken);
        return Ok(result);
    }
}
