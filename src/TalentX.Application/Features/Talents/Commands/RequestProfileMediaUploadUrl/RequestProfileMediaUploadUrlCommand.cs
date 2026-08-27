using MediatR;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Talents.Commands.RequestProfileMediaUploadUrl;

public sealed record RequestProfileMediaUploadUrlCommand(string ContentType) : IRequest<PresignedUploadTarget>;
