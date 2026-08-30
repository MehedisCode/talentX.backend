using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;
using TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;
using TalentX.Application.Features.Talents.Commands.DeleteEducation;
using TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;
using TalentX.Application.Features.Talents.Commands.ExtractResume;
using TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;
using TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;
using TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;
using TalentX.Application.Features.Talents.Commands.UpdateTalentProfile;
using TalentX.Application.Features.Talents.Commands.UpsertEducation;
using TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;
using TalentX.Application.Features.Talents.Queries.GetTalentApplicationPrefill;
using TalentX.Application.Features.Talents.Queries.GetTalentProfile;
using TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.FileStorage;

namespace TalentX.API.Controllers.Talents;

[ApiController]
[Route("api/v1/talents")]
[Authorize(Roles = nameof(UserRole.Talent))]
public sealed class TalentsController : BaseApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var profile = await Sender.Send(new GetTalentProfileQuery(), cancellationToken);
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
        var prefill = await Sender.Send(new GetTalentApplicationPrefillQuery(), cancellationToken);
        return Ok(prefill);
    }

    [HttpPost("me/resume/extract")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ExtractResume(IFormFile file, CancellationToken cancellationToken)
    {
        var command = new ExtractResumeCommand(new FormFileUploadedFile(file));
        var extractionResult = await Sender.Send(command, cancellationToken);
        return Ok(extractionResult);
    }

    // The command carries a non-JSON-friendly IUploadedFile alongside the edited skills/
    // work-experience/education fields, and ASP.NET Core cannot bind that nested structure
    // from individual multipart form fields. So the non-file fields travel as a single JSON
    // string form field ("payload"), deserialized here into ApplyExtractedProfilePayload,
    // while the file travels as its own form part — the controller still only builds the
    // command from the request and dispatches it, nothing more.
    [HttpPost("me/profile/apply-extraction")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ApplyExtractedProfile(
        IFormFile file,
        [FromForm] string payload,
        CancellationToken cancellationToken)
    {
        ApplyExtractedProfilePayload? parsedPayload;
        try
        {
            parsedPayload = JsonSerializer.Deserialize<ApplyExtractedProfilePayload>(
                payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            throw new BadRequestException("The apply-extraction payload is malformed.");
        }

        if (parsedPayload is null)
            throw new BadRequestException("The apply-extraction payload is required.");

        var command = new ApplyExtractedProfileCommand(
            parsedPayload.TechnicalSkills,
            parsedPayload.SoftSkills,
            parsedPayload.ToolsAndPlatforms,
            parsedPayload.WorkExperiences,
            parsedPayload.Educations,
            new FormFileUploadedFile(file));

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
