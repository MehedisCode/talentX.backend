using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.DTOs;

namespace TalentX.Application.Features.Talents.Queries.GetTalentMatches;
public sealed record GetTalentMatchesQuery(
    int Page = 1,
    int PageSize = 10,
    int MinScore = 50) : IRequest<PaginatedList<TalentMatchDto>>;
