using MediatR;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Commands.RespondToInvitation;

public sealed record RespondToInvitationCommand(Guid InvitationId, bool Accept) : IRequest<JobInvitationDto>;
