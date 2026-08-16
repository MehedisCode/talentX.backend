using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.CreateEmployer;
using TalentX.Application.Features.Employers.Commands.GoogleSignIn;
using TalentX.Application.Features.Employers.Commands.ResendOtp;
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
    /// <response code="201">Employer successfully created or OTP refreshed.</response>
    /// <response code="400">Request validation failed.</response>
    /// <response code="409">Employer with given email is already registered and verified.</response>
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
    /// Registers or signs in an employer account via verified Google OAuth ID token.
    /// </summary>
    /// <param name="command">Payload containing the Google ID token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verified employer profile data transfer object.</returns>
    /// <response code="200">Employer profile retrieved or created successfully.</response>
    /// <response code="400">Request validation failed or missing ID token.</response>
    /// <response code="401">Google ID token is invalid, expired, or signature verification failed.</response>
    [HttpPost("google")]
    [ProducesResponseType(typeof(EmployerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleSignIn(
        [FromBody] GoogleSignInEmployerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
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

    /// <summary>
    /// Resends a fresh verification OTP email for an unverified employer account.
    /// </summary>
    /// <param name="command">Payload containing the employer's email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation message that a new OTP was sent.</returns>
    /// <response code="200">New verification OTP dispatched successfully.</response>
    /// <response code="400">Account is already verified.</response>
    /// <response code="404">Employer not found.</response>
    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendEmployerOtpCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);

        return Ok(new { message = "A new verification code has been sent to your email." });
    }
}
