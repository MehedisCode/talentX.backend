using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;
using TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;
using TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;
using TalentX.Application.Features.Talents.Commands.DeleteEducation;
using TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;
using TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.ReplaceResume;
using TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;
using TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;
using TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;
using TalentX.Application.Features.Talents.Commands.UpsertEducation;
using TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;
using TalentX.Application.Identity.Services;
using TalentX.Infrastructure.FileStorage;

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

    [HttpPost("me/resume")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ReplaceResume(IFormFile file, CancellationToken cancellationToken)
    {
        var command = new ReplaceResumeCommand(new FormFileUploadedFile(file));
        var extractionResult = await Sender.Send(command, cancellationToken);
        return Ok(extractionResult);
    }

    [HttpPost("me/profile/apply-extraction")]
    public async Task<IActionResult> ApplyExtractedProfile(
        [FromBody] ApplyExtractedProfileCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/media/upload-url")]
    public async Task<IActionResult> RequestProfileMediaUploadUrl(
        [FromBody] RequestProfileMediaUploadUrlCommand command,
        CancellationToken cancellationToken)
    {
        var target = await Sender.Send(command, cancellationToken);
        return Ok(target);
    }

    [HttpPost("me/media/confirm")]
    public async Task<IActionResult> ConfirmProfileMediaUpload(
        [FromBody] ConfirmProfileMediaUploadCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/media/multipart/initiate")]
    public async Task<IActionResult> InitiateMediaMultipartUpload(
        [FromBody] InitiateMediaMultipartUploadCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/media/multipart/part-url")]
    public async Task<IActionResult> RequestMediaMultipartPartUrl(
        [FromBody] RequestMediaMultipartPartUrlCommand command,
        CancellationToken cancellationToken)
    {
        var presignedUrl = await Sender.Send(command, cancellationToken);
        return Ok(new { presignedUrl });
    }

    [HttpGet("me/media/multipart/parts")]
    public async Task<IActionResult> ListMediaMultipartParts(
        [FromQuery] string storageKey,
        [FromQuery] string uploadId,
        CancellationToken cancellationToken)
    {
        var parts = await Sender.Send(new ListMediaMultipartPartsQuery(storageKey, uploadId), cancellationToken);
        return Ok(new { parts });
    }

    [HttpPost("me/media/multipart/complete")]
    public async Task<IActionResult> CompleteMediaMultipartUpload(
        [FromBody] CompleteMediaMultipartUploadCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/media/multipart/abort")]
    public async Task<IActionResult> AbortMediaMultipartUpload(
        [FromBody] AbortMediaMultipartUploadCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/work-experiences")]
    public async Task<IActionResult> CreateWorkExperience(
        [FromBody] UpsertWorkExperienceCommand command,
        CancellationToken cancellationToken)
    {
        var id = await Sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPut("me/work-experiences/{id:guid}")]
    public async Task<IActionResult> UpdateWorkExperience(
        Guid id,
        [FromBody] UpsertWorkExperienceCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("me/work-experiences/{id:guid}")]
    public async Task<IActionResult> DeleteWorkExperience(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteWorkExperienceCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("me/educations")]
    public async Task<IActionResult> CreateEducation(
        [FromBody] UpsertEducationCommand command,
        CancellationToken cancellationToken)
    {
        var id = await Sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    [HttpPut("me/educations/{id:guid}")]
    public async Task<IActionResult> UpdateEducation(
        Guid id,
        [FromBody] UpsertEducationCommand command,
        CancellationToken cancellationToken)
    {
        await Sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("me/educations/{id:guid}")]
    public async Task<IActionResult> DeleteEducation(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteEducationCommand(id), cancellationToken);
        return NoContent();
    }
}
