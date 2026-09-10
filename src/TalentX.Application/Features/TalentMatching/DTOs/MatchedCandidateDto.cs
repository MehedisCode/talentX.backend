namespace TalentX.Application.Features.TalentMatching.DTOs;

public sealed class MatchedCandidateDto
{
    public Guid CandidateId { get; set; }
    public string TalentUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? Location { get; set; }
    public decimal? TotalYearsExperience { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public List<string> TechnicalSkills { get; set; } = new();
    public decimal Score { get; set; }
    public string? Explanation { get; set; }
    public bool HasActiveInvitation { get; set; }
    public string? InvitationStatus { get; set; }
    public DateTime MatchedAt { get; set; }
}
