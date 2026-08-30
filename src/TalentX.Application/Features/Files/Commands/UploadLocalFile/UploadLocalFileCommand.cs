using MediatR;

namespace TalentX.Application.Features.Files.Commands.UploadLocalFile;

public sealed record UploadLocalFileCommand(
    string StorageKey,
    Stream ContentStream) : IRequest<string>;
