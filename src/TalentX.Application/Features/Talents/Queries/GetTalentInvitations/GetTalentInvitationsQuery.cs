using MediatR;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.DTOs;

namespace TalentX.Application.Features.Talents.Queries.GetTalentInvitations;
public sealed record GetTalentInvitationsQuery(
    int Page = 1,
    int PageSize = 10) : IRequest<PaginatedList<TalentInvitationDto>>;
