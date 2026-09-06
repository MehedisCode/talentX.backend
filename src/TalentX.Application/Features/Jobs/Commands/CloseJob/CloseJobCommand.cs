using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.CloseJob;

public sealed record CloseJobCommand(Guid Id) : IRequest<Unit>;
