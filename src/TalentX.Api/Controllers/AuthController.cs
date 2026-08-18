using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Auth.LogIn;
using TalentX.Application.Features.Auth.Logout;
using TalentX.Application.Features.Auth.RefreshToken;

namespace TalentX.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        ISender sender,
        ICurrentUserService currentUserService
    )
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await _sender.Send(command);

        if (!result.Success)
        {
            return Unauthorized(result.Errors);
        }

        var login = result.Data!;

        return Ok(new
        {
            login.UserId,
            login.Email,
            login.Tokens.AccessToken,
            login.Tokens.RefreshToken
        });
    }


    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command)
    {
        var result = await _sender.Send(command);

        if (!result.Success)
        {
            return Unauthorized(result.Errors);
        }

        return Ok(new
        {
            result.Data!.AccessToken,
            result.Data.RefreshToken
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutCommand command)
    {
        await _sender.Send(command);
        return Ok();
    }

    [Authorize]
    [HttpPost("my-profile")]
    public async Task<IActionResult> MyProfile()
    {
        var userId = _currentUserService.UserId;
        var email = _currentUserService.Email;
        var roles = _currentUserService.Roles;
        var isAuthenticated = _currentUserService.IsAuthenticated;

        return Ok( new {
            userId,
            email,
            roles,
            isAuthenticated
        });
    }

    // testEmail1@gmail.com
    // 12osA_&fowfoeso
}