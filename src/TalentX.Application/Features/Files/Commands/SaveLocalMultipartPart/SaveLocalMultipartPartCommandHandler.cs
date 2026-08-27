using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Files.Commands.SaveLocalMultipartPart;

public sealed class SaveLocalMultipartPartCommandHandler : IRequestHandler<SaveLocalMultipartPartCommand, UploadedPartDto>
{
    private readonly ILocalStoragePathProvider _localStoragePathProvider;

    public SaveLocalMultipartPartCommandHandler(ILocalStoragePathProvider localStoragePathProvider)
    {
        _localStoragePathProvider = localStoragePathProvider;
    }

    public async Task<UploadedPartDto> Handle(
        SaveLocalMultipartPartCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StorageKey))
        {
            throw new BadRequestException("Storage key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UploadId) ||
            request.UploadId.Contains("..") ||
            Path.GetFileName(request.UploadId) != request.UploadId)
        {
            throw new BadRequestException("Invalid upload ID.");
        }

        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var normalizedBasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullDestinationPath = Path.GetFullPath(Path.Combine(basePath, request.StorageKey));

        if (!fullDestinationPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
            !fullDestinationPath.Equals(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid storage key path.");
        }

        var stagingDirectory = Path.Combine(basePath, ".parts", request.UploadId);
        Directory.CreateDirectory(stagingDirectory);

        var partFilePath = Path.Combine(stagingDirectory, $"part-{request.PartNumber}.tmp");
        await using (var destinationStream = new FileStream(partFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await request.ContentStream.CopyToAsync(destinationStream, cancellationToken);
        }

        var fileInfo = new FileInfo(partFilePath);
        var etag = $"\"part-{request.PartNumber}\"";

        return new UploadedPartDto(request.PartNumber, etag, fileInfo.Length);
    }
}
