using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Files.Commands.CompleteLocalMultipartUpload;

public sealed class CompleteLocalMultipartUploadCommandHandler : IRequestHandler<CompleteLocalMultipartUploadCommand, Unit>
{
    private readonly IMultipartStorageService _multipartStorageService;

    public CompleteLocalMultipartUploadCommandHandler(IMultipartStorageService multipartStorageService)
    {
        _multipartStorageService = multipartStorageService;
    }

    public async Task<Unit> Handle(
        CompleteLocalMultipartUploadCommand request,
        CancellationToken cancellationToken)
    {
        var parts = request.Parts ?? Array.Empty<PartETagDto>();

        await _multipartStorageService.CompleteMultipartUploadAsync(
            request.StorageKey,
            request.UploadId,
            parts,
            cancellationToken);

        return Unit.Value;
    }
}
