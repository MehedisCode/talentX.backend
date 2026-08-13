using Microsoft.AspNetCore.Mvc;

namespace TalentX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("health")]
    public async Task<IActionResult> Health()
    {
        return Ok("Healthy!");
    }
}