using MediatR;

namespace TalentX.Application.Features.Talents.Commands.DeleteEducation;

public sealed record DeleteEducationCommand(Guid Id) : IRequest;
