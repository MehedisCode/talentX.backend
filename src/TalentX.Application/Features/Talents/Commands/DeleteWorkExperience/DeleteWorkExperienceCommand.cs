using MediatR;

namespace TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;

public sealed record DeleteWorkExperienceCommand(Guid Id) : IRequest;
