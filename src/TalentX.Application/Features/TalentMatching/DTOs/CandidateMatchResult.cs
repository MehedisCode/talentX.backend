namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class CandidateMatchResult
{
    public string TalentUserId { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Explanation { get; set; }
}
