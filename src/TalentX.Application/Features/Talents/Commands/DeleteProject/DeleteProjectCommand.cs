using MediatR;

namespace TalentX.Application.Features.Talents.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid Id) : IRequest;
