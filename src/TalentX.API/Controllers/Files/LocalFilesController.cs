using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Files.Commands.AbortLocalMultipartUpload;
using TalentX.Application.Features.Files.Commands.CompleteLocalMultipartUpload;
using TalentX.Application.Features.Files.Commands.SaveLocalMultipartPart;
using TalentX.Application.Features.Files.Commands.UploadLocalFile;
using TalentX.Application.Features.Files.Queries.GetLocalFileStream;

namespace TalentX.API.Controllers.Files;

[Route("api/v1/files")]
public sealed class LocalFilesController : BaseApiController
{
    private readonly IHostEnvironment _hostEnvironment;

    public LocalFilesController(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    private bool IsAllowedEnvironment() =>
        _hostEnvironment.IsDevelopment() || _hostEnvironment.IsEnvironment("Testing");

    [HttpGet("stream")]
    public async Task<IActionResult> Stream(
        [FromQuery] string key,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedEnvironment())
        {
            return NotFound();
        }

        var query = new GetLocalFileStreamQuery(key);
        var result = await Sender.Send(query, cancellationToken);

        return File(result.Stream, result.ContentType, enableRangeProcessing: true);
    }

    [HttpPut("upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(
        [FromQuery] string key,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedEnvironment())
        {
            return NotFound();
        }

        var command = new UploadLocalFileCommand(key, Request.Body);
        var storageKey = await Sender.Send(command, cancellationToken);

        return Ok(new { StorageKey = storageKey });
    }

    [HttpPut("multipart/part")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadPart(
        [FromQuery] string key,
        [FromQuery] string uploadId,
        [FromQuery] int partNumber,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedEnvironment())
        {
            return NotFound();
        }

        var command = new SaveLocalMultipartPartCommand(key, uploadId, partNumber, Request.Body);
        var result = await Sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("multipart/complete")]
    public async Task<IActionResult> CompleteMultipart(
        [FromQuery] string key,
        [FromQuery] string uploadId,
        [FromBody] IReadOnlyList<PartETagDto>? parts,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedEnvironment())
        {
            return NotFound();
        }

        var command = new CompleteLocalMultipartUploadCommand(key, uploadId, parts);
        await Sender.Send(command, cancellationToken);

        return Ok(new { StorageKey = key, UploadId = uploadId, Status = "Completed" });
    }

    [HttpDelete("multipart/abort")]
    public async Task<IActionResult> AbortMultipart(
        [FromQuery] string key,
        [FromQuery] string uploadId,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedEnvironment())
        {
            return NotFound();
        }

        var command = new AbortLocalMultipartUploadCommand(key, uploadId);
        await Sender.Send(command, cancellationToken);

        return NoContent();
    }
}
