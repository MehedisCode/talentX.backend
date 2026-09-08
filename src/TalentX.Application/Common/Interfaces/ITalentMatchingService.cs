using TalentX.Application.Features.TalentMatching.DTOs;

namespace TalentX.Application.Common.Interfaces;

public interface ITalentMatchingService
{
    Task<TalentMatchingResult> MatchAsync(
        TalentMatchingRequest request,
        CancellationToken cancellationToken = default);
}
