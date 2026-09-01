using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.CreateEmployer;
using TalentX.Application.Features.Employers.Commands.GoogleSignIn;
using TalentX.Application.Features.Employers.Commands.SendOtp;
using TalentX.Application.Features.Employers.Commands.VerifyEmail;
using TalentX.Application.Identity.Services;

namespace TalentX.API.Controllers.Employers;

/// <summary>
/// Handles employer onboarding, registration, Google authentication, and email OTP verification.
/// </summary>
[ApiController]
[Route("api/v1/employers")]
public sealed class EmployersController : BaseApiController
{
    /// <summary>
    /// Registers a new employer with an email, company name, and password. Dispatches an OTP verification email.
    /// </summary>
    [HttpPost]
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthEmployerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmployer(
        [FromBody] CreateEmployerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreateEmployer), new { id = result.Employer?.Id }, result);
    }

    /// <summary>
    /// Authenticates or registers an employer using a Google OAuth ID token.
    /// </summary>
    [HttpPost("google")]
    [HttpPost("google-signin")]
    [AllowAnonymous]
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
    /// Verifies the employer's email using the 6-digit OTP code received via email and returns JWT session tokens.
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthEmployerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmployerEmailCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sends a 6-digit OTP verification code to the specified email address.
    /// </summary>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendEmployerOtpCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return Ok(new { message = "A verification code has been sent to your email." });
    }

    /// <summary>
    /// Returns the current authenticated employer's profile including company name.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentEmployerResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile(
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IEmployerRepository employerRepository,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return Unauthorized();

        var employer = await employerRepository.GetByUserIdAsync(userId, cancellationToken);
        if (employer is null)
            return NotFound(new { message = "Employer profile not found." });

        return Ok(new CurrentEmployerResult
        {
            Id = employer.Id.ToString(),
            CompanyName = employer.CompanyName
        });
    }
}

public sealed class CurrentEmployerResult
{
    public string Id { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
