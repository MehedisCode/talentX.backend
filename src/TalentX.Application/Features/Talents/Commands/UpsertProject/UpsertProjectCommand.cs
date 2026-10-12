using MediatR;

namespace TalentX.Application.Features.Talents.Commands.UpsertProject;

public sealed record UpsertProjectCommand : IRequest<Guid>
{
    public Guid? Id { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string ProjectType { get; init; } = string.Empty;
    public string StartDate { get; init; } = string.Empty;
    public string? EndDate { get; init; }
    public bool IsOngoing { get; init; }
    public string Description { get; init; } = string.Empty;
    public List<string>? TechnologiesUsed { get; init; }
    public string? ProjectUrl { get; init; }
}
