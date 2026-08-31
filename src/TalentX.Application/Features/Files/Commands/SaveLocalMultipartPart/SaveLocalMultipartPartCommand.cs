using MediatR;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Files.Commands.SaveLocalMultipartPart;

public sealed record SaveLocalMultipartPartCommand(
    string StorageKey,
    string UploadId,
    int PartNumber,
    Stream ContentStream) : IRequest<UploadedPartDto>;
