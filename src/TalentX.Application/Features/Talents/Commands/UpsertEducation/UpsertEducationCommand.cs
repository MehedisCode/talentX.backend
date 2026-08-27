using MediatR;

namespace TalentX.Application.Features.Talents.Commands.UpsertEducation;

public sealed record UpsertEducationCommand(
    Guid? Id,
    string Institution,
    string Degree,
    string? StartDate,
    string? EndDate
) : IRequest<Guid>;
