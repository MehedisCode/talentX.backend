namespace TalentX.Infrastructure.Ai.Models;

public sealed class GeminiProject
{
    public string? ProjectName { get; set; }
    public string? Role { get; set; }
    public string? ProjectType { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public bool IsOngoing { get; set; }
    public string? Description { get; set; }
    public List<string>? TechnologiesUsed { get; set; }
    public string? ProjectUrl { get; set; }
}
