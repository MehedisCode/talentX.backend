namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class TalentMatchingResult
{
    public List<CandidateMatchResult> Matches { get; set; } = new();
}
