using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.CreateEmployer;
using TalentX.Application.Features.Employers.Commands.VerifyEmail;

namespace TalentX.Api.Controllers;

/// <summary>
/// Handles endpoints for employer account operations and management.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public sealed class EmployersController : ControllerBase
{
    private readonly ISender _sender;

    public EmployersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers and creates a new employer profile.
    /// </summary>
    /// <param name="command">The employer registration command details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created employer data transfer object.</returns>
    /// <response code="201">Employer successfully created.</response>
    /// <response code="400">Request validation failed.</response>
    /// <response code="409">Employer with given email already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EmployerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Verifies the employer's email using a 6-digit OTP code.
    /// </summary>
    /// <param name="command">Verification payload containing email and OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation message of successful verification.</returns>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="400">Invalid or expired OTP code.</response>
    /// <response code="404">Employer not found.</response>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmployerEmailCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);

        return Ok(new { message = "Email verified successfully." });
    }
}
