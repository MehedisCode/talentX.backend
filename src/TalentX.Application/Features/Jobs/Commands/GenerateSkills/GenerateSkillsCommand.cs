using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.GenerateSkills;

/// <summary>
/// Command to generate relevant skills for a job using AI.
/// Used in the multi-step wizard when employer clicks "Generate Skills".
/// </summary>
public sealed record GenerateSkillsCommand : IRequest<GenerateSkillsResponse>
{
    public string Title { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public string WorkArrangement { get; init; } = string.Empty;
}

/// <summary>
/// Response containing the AI-generated skills list.
/// </summary>
public sealed record GenerateSkillsResponse(List<string> Skills);
