

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Identity.Models;
using TalentX.Application.Features.Auth.LogIn;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        ISender sender,
        IOptions<JwtSettings> jwtSettings)
    {
        _sender = sender;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginCommand command)
    {
        var result = await _sender.Send(command);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        var tokens = (TokenResponse)result.Data!;

        SetAuthCookies(tokens);

        return Ok();
    }

    private void SetAuthCookies(TokenResponse tokens)
    {
        Response.Cookies.Append(
            "accessToken",
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
            "refreshToken",
            tokens.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(
                    _jwtSettings.RefreshTokenExpiryDays),
                Path = "/api/auth"
            });
    }
}