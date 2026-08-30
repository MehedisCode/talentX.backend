using MediatR;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Files.Commands.CompleteLocalMultipartUpload;

public sealed record CompleteLocalMultipartUploadCommand(
    string StorageKey,
    string UploadId,
    IReadOnlyList<PartETagDto>? Parts = null) : IRequest<Unit>;
