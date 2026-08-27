using MediatR;

namespace TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;

public sealed record RequestMediaMultipartPartUrlCommand(
    string StorageKey,
    string UploadId,
    int PartNumber
) : IRequest<string>;
