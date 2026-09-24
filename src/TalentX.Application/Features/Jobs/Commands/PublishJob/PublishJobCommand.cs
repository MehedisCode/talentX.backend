using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.PublishJob;

public sealed record PublishJobCommand(Guid Id) : IRequest<Unit>;
