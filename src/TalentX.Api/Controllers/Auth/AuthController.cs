using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Features.Auth.CurrentUser;
using TalentX.Application.Features.Auth.LogIn;
using TalentX.Application.Features.Auth.Logout;
using TalentX.Application.Features.Auth.RefreshToken;

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


    [Authorize]
    [HttpGet("user")]
    public async Task<IActionResult> CurrentUser()
    {
        var result = await Sender.Send(new CurrentUserQuery());

        if (!result.Success)
            return Unauthorized(result.Errors);

        return Ok(result.Data);
    }
}