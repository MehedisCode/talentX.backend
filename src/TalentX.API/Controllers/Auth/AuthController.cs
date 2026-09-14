using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Auth.Commands.VerifyGuestEmail;
using TalentX.Application.Features.Auth.AddPassword;
using TalentX.Application.Features.Auth.ChangePassword;
using TalentX.Application.Features.Auth.CurrentUser;
using TalentX.Application.Features.Auth.DeleteAccount;
using TalentX.Application.Features.Auth.ForgotPassword;
using TalentX.Application.Features.Auth.Login;
using TalentX.Application.Features.Auth.Logout;
using TalentX.Application.Features.Auth.RefreshToken;
using TalentX.Application.Features.Auth.ResetPassword;
using TalentX.Application.Features.Auth.ValidateResetToken;

namespace TalentX.API.Controllers.Auth;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await Sender.Send(command);

        if (!result.Success)
            return Unauthorized(result.Errors);

        var login = result.Data!;

        return Ok(new
        {
            login.UserId,
            login.Email,
            login.Tokens.AccessToken,
            login.Tokens.RefreshToken
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command)
    {
        var result = await Sender.Send(command);

        if (!result.Success)
            return Unauthorized(result.Errors);

        return Ok(new
        {
            result.Data!.AccessToken,
            result.Data.RefreshToken
        });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutCommand command)
    {
        await Sender.Send(command);
        return Ok();
    }

    [Authorize(Roles = $"{nameof(UserRole.Employer)},{nameof(UserRole.Talent)},{nameof(UserRole.Guest)}")]
    [HttpGet("user")]
    public async Task<IActionResult> CurrentUser()
    {
        var result = await Sender.Send(new CurrentUserQuery());

        if (!result.Success)
            return Unauthorized(result.Errors);

        return Ok(result.Data);
    }

    [Authorize(Roles = $"{nameof(UserRole.Employer)},{nameof(UserRole.Talent)},{nameof(UserRole.Guest)}")]    
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command)
    {
        var result = await Sender.Send(command);

        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Incorrect password."))
            {
                return BadRequest(new { message = "Incorrect password." });
            }
            return BadRequest(result.Errors);
        }

        return Ok(new { message = "Password changed successfully." });
    }

    [Authorize]
    [HttpPost("add-password")]
    public async Task<IActionResult> AddPassword([FromBody] AddPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await Sender.Send(new AddPasswordCommand(request.NewPassword, request.ConfirmPassword, request.CurrentRefreshToken), cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("validate-reset-token")]
    public async Task<ActionResult<ValidateResetTokenResult>> ValidateResetToken(
        [FromQuery] string email,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ValidateResetTokenQuery(email, token), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await Sender.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword, request.ConfirmPassword), cancellationToken);
        return NoContent();
    }


    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyGuestEmail(VerifyGuestEmailCommand command)
    {
        var result = await Sender.Send(command);

        return Ok(new
        {
            result.UserId,
            result.Email,
            result.AccessToken,
            result.RefreshToken
        });
    }

    [Authorize(Roles = $"{nameof(UserRole.Employer)},{nameof(UserRole.Talent)}")]
    [HttpPost("delete-account/send-otp")]
    public async Task<IActionResult> SendDeleteAccountOtp(CancellationToken cancellationToken)
    {
        await Sender.Send(new SendDeleteAccountOtpCommand(), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = $"{nameof(UserRole.Employer)},{nameof(UserRole.Talent)}")]
    [HttpPost("delete-account")]
    public async Task<ActionResult<DeleteAccountResult>> DeleteAccount(
        [FromBody] DeleteAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

