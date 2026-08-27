using MediatR;

namespace TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;

public sealed record AbortMediaMultipartUploadCommand(
    string StorageKey,
    string UploadId
) : IRequest;
