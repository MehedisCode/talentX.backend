using MediatR;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Commands.WithdrawInvitation;

public sealed record WithdrawInvitationCommand(Guid JobId, Guid InvitationId) : IRequest<JobInvitationDto>;
