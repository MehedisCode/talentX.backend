using MediatR;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Queries.GetJobInvitations;

public sealed record GetJobInvitationsQuery(Guid JobId) : IRequest<List<JobInvitationDto>>;
