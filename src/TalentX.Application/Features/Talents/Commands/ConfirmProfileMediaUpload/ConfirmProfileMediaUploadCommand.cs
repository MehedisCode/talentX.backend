using MediatR;

namespace TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;

public sealed record ConfirmProfileMediaUploadCommand(string StorageKey) : IRequest;
