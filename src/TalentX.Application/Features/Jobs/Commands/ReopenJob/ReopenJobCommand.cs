using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.ReopenJob;

public sealed record ReopenJobCommand(Guid Id) : IRequest<Unit>;
