using MediatR;

namespace TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;

public sealed record CreateTalentForTestingCommand(
    string UserId,
    string Name,
    string Email
) : IRequest<Guid>;
