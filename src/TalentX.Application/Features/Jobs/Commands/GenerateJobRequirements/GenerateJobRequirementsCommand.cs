using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.GenerateJobRequirements;

/// <summary>
/// Command to generate a job's requirements section using AI based on basic
/// job details. Used in the multi-step wizard when employer clicks
/// "Generate Requirements".
/// </summary>
public sealed record GenerateJobRequirementsCommand : IRequest<GenerateJobRequirementsResponse>
{
    public string Title { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public string WorkArrangement { get; init; } = string.Empty;
    public List<string>? RequiredSkills { get; init; }
}

/// <summary>
/// Response containing the AI-generated job requirements bullet list.
/// </summary>
public sealed record GenerateJobRequirementsResponse(string Requirements);
