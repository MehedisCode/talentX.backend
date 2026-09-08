using MediatR;

namespace TalentX.Application.Features.Invitations.Commands.SendJobInvitation;

public sealed record SendJobInvitationCommand(Guid JobId, string TalentUserId) : IRequest<SendJobInvitationResponse>;
