using MediatR;

namespace TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;

public sealed record CompleteMediaMultipartUploadCommand(
    string StorageKey,
    string UploadId,
    List<CompleteMediaMultipartUploadPartItem> Parts
) : IRequest;
