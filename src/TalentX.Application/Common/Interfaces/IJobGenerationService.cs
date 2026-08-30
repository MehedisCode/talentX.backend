namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Service for generating job descriptions and skills using AI.
/// Used by the multi-step job creation wizard to assist employers.
/// </summary>
public interface IJobGenerationService
{
    /// <summary>
    /// Generates a job description based on job details.
    /// </summary>
    Task<JobGenerationResult> GenerateDescriptionAsync(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement,
        List<string>? requiredSkills = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates relevant skills for a job based on its details.
    /// </summary>
    Task<JobSkillsResult> GenerateSkillsAsync(
        string title,
        string companyName,
        string employmentType,
        string experienceLevel,
        string workArrangement,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of AI-generated job description.
/// </summary>
public sealed class JobGenerationResult
{
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Result of AI-generated job skills.
/// </summary>
public sealed class JobSkillsResult
{
    public List<string> Skills { get; init; } = new();
}
