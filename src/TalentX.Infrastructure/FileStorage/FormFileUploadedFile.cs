using Microsoft.AspNetCore.Http;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.FileStorage;

/// <summary>
/// Concrete implementation of IUploadedFile that wraps Microsoft.AspNetCore.Http.IFormFile.
/// </summary>
public sealed class FormFileUploadedFile : IUploadedFile
{
    private readonly IFormFile _formFile;

    public FormFileUploadedFile(IFormFile formFile)
    {
        _formFile = formFile ?? throw new ArgumentNullException(nameof(formFile));
    }

    public string FileName => _formFile.FileName;
    public string ContentType => _formFile.ContentType;
    public long Length => _formFile.Length;
    public Stream OpenReadStream() => _formFile.OpenReadStream();
}
