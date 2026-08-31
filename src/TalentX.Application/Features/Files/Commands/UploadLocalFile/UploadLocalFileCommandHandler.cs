using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Files.Commands.UploadLocalFile;

public sealed class UploadLocalFileCommandHandler : IRequestHandler<UploadLocalFileCommand, string>
{
    private readonly ILocalStoragePathProvider _localStoragePathProvider;

    public UploadLocalFileCommandHandler(ILocalStoragePathProvider localStoragePathProvider)
    {
        _localStoragePathProvider = localStoragePathProvider;
    }

    public async Task<string> Handle(
        UploadLocalFileCommand request,
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

        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var destinationFileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await request.ContentStream.CopyToAsync(destinationFileStream, cancellationToken);

        return request.StorageKey;
    }
}
