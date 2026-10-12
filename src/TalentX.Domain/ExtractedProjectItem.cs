namespace TalentX.Domain;

public sealed class ExtractedProjectItem
{
    public string? ProjectName { get; init; }
    public string? Role { get; init; }
    public string? ProjectType { get; init; }
    public string? StartDate { get; init; }
    public string? EndDate { get; init; }
    public bool IsOngoing { get; init; }
    public string? Description { get; init; }
    public List<string>? TechnologiesUsed { get; init; }
    public string? ProjectUrl { get; init; }
}
