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
using TalentX.Application.Features.EmployerVerification.Commands.ReApply;
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
            CompanyName = employer.CompanyName,
            VerificationStatus = employer.VerificationStatus,
            RejectionReason = employer.RejectionReason
        });
    }

    /// <summary>
    /// Re-applies for employer verification after a rejection. Resets status to PendingVerification.
    /// </summary>
    [HttpPost("me/re-apply")]
    [Authorize]
    public async Task<IActionResult> ReApply(CancellationToken cancellationToken)
    {
        await Sender.Send(new ReApplyCommand(), cancellationToken);
        return Ok(new { message = "Re-application submitted. Your account is now pending verification." });
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await Sender.Send(new GetEmployerDashboardQuery(), cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("jobs")]
    [Authorize(Roles = $"{nameof(UserRole.Employer)}")]
    public async Task<IActionResult> GetJobs(
        [FromQuery] GetEmployerJobsQueryParams? queryParams = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = queryParams ?? new GetEmployerJobsQueryParams();
        var query = new GetEmployerJobsQuery(
            parameters.Page,
            parameters.PageSize,
            parameters.Status,
            parameters.IsExpired,
            parameters.SearchTerm);
        var jobs = await Sender.Send(query, cancellationToken);
        return Ok(jobs);
    }
}
