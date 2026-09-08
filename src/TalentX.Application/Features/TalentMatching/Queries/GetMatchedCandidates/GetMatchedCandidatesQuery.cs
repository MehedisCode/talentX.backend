using MediatR;
using TalentX.Application.Features.TalentMatching.DTOs;

namespace TalentX.Application.Features.TalentMatching.Queries.GetMatchedCandidates;

public sealed record GetMatchedCandidatesQuery(Guid JobId) : IRequest<List<MatchedCandidateDto>>;
