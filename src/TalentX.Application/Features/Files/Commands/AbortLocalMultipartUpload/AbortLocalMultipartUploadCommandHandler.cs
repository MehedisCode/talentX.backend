using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Files.Commands.AbortLocalMultipartUpload;

public sealed class AbortLocalMultipartUploadCommandHandler : IRequestHandler<AbortLocalMultipartUploadCommand, Unit>
{
    private readonly IMultipartStorageService _multipartStorageService;

    public AbortLocalMultipartUploadCommandHandler(IMultipartStorageService multipartStorageService)
    {
        _multipartStorageService = multipartStorageService;
    }

    public async Task<Unit> Handle(
        AbortLocalMultipartUploadCommand request,
        CancellationToken cancellationToken)
    {
        await _multipartStorageService.AbortMultipartUploadAsync(
            request.StorageKey,
            request.UploadId,
            cancellationToken);

        return Unit.Value;
    }
}
