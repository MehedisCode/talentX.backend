using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Identity.Models;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Auth.Login;
using TalentX.Application.Features.Auth.Logout;
using TalentX.Application.Features.Auth.RefreshToken;
using TalentX.Infrastructure.Auth;

namespace TalentX.Api.Controllers.Identity;

public class AuthController : VersionedApiController
{
    private readonly JwtSettings _jwtSettings;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IOptions<JwtSettings> jwtSettings,
        IIdentityService identityService,
        ICurrentUserService currentUserService
    )
    {
        _jwtSettings = jwtSettings.Value;
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await Mediator.Send(command);

        if (!result.Success)
        {
            return Unauthorized(result.Errors);
        }

        var login = result.Data!;

        SetAuthCookies(login.Tokens);

        return Ok(new
        {
            login.UserId,
            login.Email
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["TalentXRefreshToken"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await Mediator.Send(new LogoutCommand(refreshToken));
        }

        ClearAuthCookies();

        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["TalentXRefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized("No refresh token provided.");
        }

        var result = await Mediator.Send(new RefreshTokenCommand(refreshToken));

        if (!result.Success)
        {
            ClearAuthCookies();
            return Unauthorized(result.Errors);
        }

        SetAuthCookies(result.Data!);

        return Ok();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {
        var result = await _identityService.CreateApplicationUserAsync(
            "testEmail2@gmail.com",
            "12osA_&fowfoeso",
            Domain.Enums.UserRole.Employer);

        if (!result.Success)
        {
           return StatusCode(StatusCodes.Status500InternalServerError, result.Errors);
        }

        return Ok(result.Data);
    }

    [HttpPost("my-profile")]
    public Task<IActionResult> MyProfile()
    {
        var userId = _currentUserService.UserId;
        var email = _currentUserService.Email;
        var roles = _currentUserService.Roles;
        var isAuthenticated = _currentUserService.IsAuthenticated;

        return Task.FromResult<IActionResult>(Ok(new {
            userId,
            email,
            roles,
            isAuthenticated
        }));
    }

    private void SetAuthCookies(TokenResponse tokens)
    {
        Response.Cookies.Append(
            "TalentXAccessToken",
            tokens.AccessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(
                    _jwtSettings.AccessTokenMinutes),
                Path = "/"
            });

        Response.Cookies.Append(
            "TalentXRefreshToken",
            tokens.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(
                    _jwtSettings.RefreshTokenExpiryDays),
                Path = "/"
            });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("TalentXAccessToken", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("TalentXRefreshToken", new CookieOptions { Path = "/" });
    }
}
