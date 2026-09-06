using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.DTOs.Employers;
using TalentX.Application.Features.Employers.Commands.CreateEmployer;
using TalentX.Application.Features.Employers.Commands.GoogleSignIn;
using TalentX.Application.Features.Employers.Commands.SendOtp;
using TalentX.Application.Features.Employers.Commands.VerifyEmail;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Features.Employers.Queries.GetEmployerDashboard;
using TalentX.Application.Features.Employers.Queries.GetEmployerJobs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

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

    [HttpGet("dashboard")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    [ProducesResponseType(typeof(EmployerDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await Sender.Send(new GetEmployerDashboardQuery(), cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("jobs")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    [ProducesResponseType(typeof(PaginatedList<EmployerJobListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = PaginationRequest.DefaultPageNumber,
        [FromQuery] int pageSize = EmployerDashboardConstants.DefaultJobsPageSize,
        [FromQuery] JobStatus? status = null,
        [FromQuery] bool? isExpired = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEmployerJobsQuery(page, pageSize, status, isExpired);
        var jobs = await Sender.Send(query, cancellationToken);
        return Ok(jobs);
    }
}

public sealed class CurrentEmployerResult
{
    public string Id { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
