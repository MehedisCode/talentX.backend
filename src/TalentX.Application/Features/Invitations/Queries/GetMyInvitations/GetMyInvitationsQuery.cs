using MediatR;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Queries.GetMyInvitations;

public sealed record GetMyInvitationsQuery : IRequest<List<JobInvitationDto>>;
