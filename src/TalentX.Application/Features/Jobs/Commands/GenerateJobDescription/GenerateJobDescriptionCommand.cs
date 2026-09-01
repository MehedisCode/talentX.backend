using MediatR;

namespace TalentX.Application.Features.Jobs.Commands.GenerateJobDescription;

/// <summary>
/// Command to generate a job description using AI based on basic job details.
/// Used in the multi-step wizard when employer clicks "Generate Description".
/// </summary>
public sealed record GenerateJobDescriptionCommand : IRequest<GenerateJobDescriptionResponse>
{
    public string Title { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string EmploymentType { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public string WorkArrangement { get; init; } = string.Empty;
    public List<string>? RequiredSkills { get; init; }
}

/// <summary>
/// Response containing the AI-generated job description.
/// </summary>
public sealed record GenerateJobDescriptionResponse(string Description);
