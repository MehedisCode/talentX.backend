namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Abstraction for uploaded files to keep application logic decoupled from ASP.NET Core IFormFile.
/// </summary>
public interface IUploadedFile
{
    /// <summary>
    /// Gets the original name of the uploaded file.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Gets the MIME content type of the uploaded file.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the length of the uploaded file in bytes.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Opens a read stream to access the file content.
    /// </summary>
    Stream OpenReadStream();
}
