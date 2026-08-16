using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Identity.Models;
using TalentX.Application.Features.Auth.LogIn;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly JwtSettings _jwtSettings;

    private readonly IIdentityService _identityService;

    public AuthController(
        ISender sender,
        IOptions<JwtSettings> jwtSettings,
        IIdentityService identityService
    )
    {
        _sender = sender;
        _jwtSettings = jwtSettings.Value;
        _identityService = identityService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginCommand command)
    {
        var result = await _sender.Send(command);

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


    [HttpPost("register")]
    public async Task<IActionResult> Register()
    {

        // var result = await _identityService.CreateApplicationUserAsync(
        //     "testEmail1@gmail.com",
        //     "12osA_&fowfoeso",
        //     Domain.Enums.UserRole.Employer);

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
}