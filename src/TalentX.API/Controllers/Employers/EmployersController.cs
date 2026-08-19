using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.CreateEmployer;
using TalentX.Application.Features.Employers.Commands.GoogleSignIn;
using TalentX.Application.Features.Employers.Commands.ResendOtp;
using TalentX.Application.Features.Employers.Commands.VerifyEmail;

namespace TalentX.API.Controllers.Employers;

/// <summary>
/// Handles employer onboarding, registration, Google authentication, and email OTP verification.
/// </summary>
[ApiController]
[Route("api/v1/employers")]
[AllowAnonymous]
public sealed class EmployersController : BaseApiController
{
    /// <summary>
    /// Registers a new employer with an email, company name, and password. Dispatches an OTP verification email.
    /// </summary>
    /// <param name="command">Registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created employer details.</returns>
    /// <response code="201">Employer created successfully.</response>
    /// <response code="400">Validation errors.</response>
    /// <response code="409">An employer with this email is already registered.</response>
    [HttpPost]
    [HttpPost("register")]
    [ProducesResponseType(typeof(EmployerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmployer(
        [FromBody] CreateEmployerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreateEmployer), new { id = result.Id }, result);
    }

    /// <summary>
    /// Authenticates or registers an employer using a Google OAuth ID token.
    /// </summary>
    /// <param name="command">Google OAuth payload containing the raw ID token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The employer details.</returns>
    /// <response code="200">Google sign-in successful.</response>
    /// <response code="400">Invalid Google ID token.</response>
    /// <response code="401">Unauthorized Google token.</response>
    [HttpPost("google")]
    [HttpPost("google-signin")]
    [ProducesResponseType(typeof(AuthEmployerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleSignIn(
        [FromBody] GoogleSignInEmployerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Verifies the employer's email using the 6-digit OTP code received via email.
    /// </summary>
    /// <param name="command">Verification payload containing email and OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="400">Invalid or expired OTP code.</response>
    /// <response code="404">Employer not found.</response>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmployerEmailCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return Ok(new { message = "Email verified successfully." });
    }

    /// <summary>
    /// Resends a fresh 6-digit OTP code to the employer's email address.
    /// </summary>
    /// <param name="command">Payload containing the employer's email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">OTP code dispatched successfully.</response>
    /// <response code="400">Employer is already verified.</response>
    /// <response code="404">Employer not found.</response>
    [HttpPost("resend-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendEmployerOtpCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return Ok(new { message = "A new verification code has been sent to your email." });
    }
}
