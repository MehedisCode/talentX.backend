using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TalentX.API.Controllers;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Resumes.Commands.UploadResume;
using TalentX.Infrastructure.FileStorage;

namespace TalentX.API.Controllers.Resume;


/// <summary>
/// Handles resume upload and AI-powered extraction.
/// </summary>
[Route("api/v1/[controller]")]
public sealed class ResumesController : BaseApiController
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadResumeCommand(new FormFileUploadedFile(file));
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

