using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.DeleteJob;

public sealed record DeleteJobCommand(Guid Id) : IRequest<Unit>;
