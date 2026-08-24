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
    /// <summary>
    /// Uploads a resume (PDF or DOCX) and returns structured extracted data.
    /// </summary>
    /// <param name="file">The resume file (PDF or DOCX, max 5 MB).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Structured resume data extracted by AI.</returns>
    /// <response code="200">Extraction succeeded. Partial nulls in optional fields are expected.</response>
    /// <response code="400">Invalid file type, extension mismatch, or file exceeds 5 MB.</response>
    /// <response code="422">File is valid but contains no extractable text (e.g. scanned PDF).</response>
    /// <response code="502">Upstream service failure (Gemini API or file storage).</response>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(ResumeExtractionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadResumeCommand(new FormFileUploadedFile(file));
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

