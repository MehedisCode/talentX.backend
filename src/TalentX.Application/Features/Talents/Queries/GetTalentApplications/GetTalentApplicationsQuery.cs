using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.DTOs;

namespace TalentX.Application.Features.Talents.Queries.GetTalentApplications;
public sealed record GetTalentApplicationsQuery(
    string? Status = null,
    int Page = 1,
    int PageSize = 10) : IRequest<PaginatedList<TalentApplicationDto>>;
