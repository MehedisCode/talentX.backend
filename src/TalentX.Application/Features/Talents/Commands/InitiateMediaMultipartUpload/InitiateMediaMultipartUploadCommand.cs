using MediatR;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Talents.Commands.InitiateMediaMultipartUpload;

public sealed record InitiateMediaMultipartUploadCommand(string ContentType) : IRequest<InitiateMultipartResult>;
