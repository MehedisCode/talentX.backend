namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class TalentMatchingRequest
{
    public JobProfileForMatching Job { get; set; } = new();
    public List<TalentProfileForMatching> Candidates { get; set; } = new();
}
