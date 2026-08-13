using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    public AuthController(
        IUnitOfWork unitOfWork
    )
    {
        _unitOfWork = unitOfWork;
    }
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        // return Ok("Healthy!");

        return Ok(await _unitOfWork.Users.GetByEmailAsync("mail@.com"));
    }
}