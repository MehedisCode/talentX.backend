namespace TalentX.Application.Features.Employers.DTOs;

public sealed record CandidatePipelineStatsDto(
    int AppliedCount,
    int ShortlistedCount,
    int InvitedCount,
    int InterviewCount,
    int RejectedCount,
    int HiredCount);

