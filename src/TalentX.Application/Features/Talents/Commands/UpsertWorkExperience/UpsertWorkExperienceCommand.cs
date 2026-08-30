using MediatR;

namespace TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;

public sealed record UpsertWorkExperienceCommand(
    Guid? Id,
    string CompanyName,
    string JobTitle,
    string? StartDate,
    string? EndDate,
    bool IsCurrent
) : IRequest<Guid>;
