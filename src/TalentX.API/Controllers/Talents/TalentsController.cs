using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;
using TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using TalentX.Application.Identity.Services;

namespace TalentX.API.Controllers.Talents;

[ApiController]
[Route("api/v1/talents")]
[AllowAnonymous]
public sealed class TalentsController : BaseApiController
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public TalentsController(ICurrentUserService currentUserService, IWebHostEnvironment webHostEnvironment)
    {
        _currentUserService = currentUserService;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your profile.");

        var profile = await Sender.Send(new GetTalentProfileQuery(userId), cancellationToken);
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateTalentProfileCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("me/application-prefill")]
    public async Task<IActionResult> GetMyApplicationPrefill(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to view your application pre-fill.");

        var prefill = await Sender.Send(new GetTalentApplicationPrefillQuery(userId), cancellationToken);
        return Ok(prefill);
    }

    [HttpPost("testing")]
    public async Task<IActionResult> CreateTalentForTesting(
        [FromBody] CreateTalentForTestingCommand command,
        CancellationToken cancellationToken)
    {
        if (!_webHostEnvironment.IsDevelopment())
            return NotFound();

        var talentId = await Sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id = talentId });
    }
}
