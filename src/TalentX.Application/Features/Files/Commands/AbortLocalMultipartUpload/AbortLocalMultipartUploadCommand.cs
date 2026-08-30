using MediatR;

namespace TalentX.Application.Features.Files.Commands.AbortLocalMultipartUpload;

public sealed record AbortLocalMultipartUploadCommand(
    string StorageKey,
    string UploadId) : IRequest<Unit>;
