using MediatR;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Queries.GetInvitationByToken;

public sealed record GetInvitationByTokenQuery(string Token) : IRequest<JobInvitationDto>;
