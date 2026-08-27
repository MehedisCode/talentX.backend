using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Files.Queries.GetLocalFileStream;

public sealed class GetLocalFileStreamQueryHandler : IRequestHandler<GetLocalFileStreamQuery, LocalFileStreamResult>
{
    private readonly ILocalStoragePathProvider _localStoragePathProvider;

    public GetLocalFileStreamQueryHandler(ILocalStoragePathProvider localStoragePathProvider)
    {
        _localStoragePathProvider = localStoragePathProvider;
    }

    public Task<LocalFileStreamResult> Handle(
        GetLocalFileStreamQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StorageKey))
        {
            throw new BadRequestException("Storage key is required.");
        }

        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var normalizedBasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(basePath, request.StorageKey));

        if (!fullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.Equals(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid storage key path.");
        }

        if (!File.Exists(fullPath))
        {
            throw new NotFoundException("File not found.");
        }

        var contentType = ResolveContentType(fullPath);
        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = Path.GetFileName(fullPath);

        var result = new LocalFileStreamResult(fileStream, contentType, fileName);
        return Task.FromResult(result);
    }

    private static string ResolveContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
