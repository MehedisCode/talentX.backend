using TalentX.Application.Features.TalentMatching.DTOs;

namespace TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;

public sealed record RunJobTalentMatchingResponse(
    Guid JobId,
    int MatchedCount,
    List<MatchedCandidateDto> Candidates);
