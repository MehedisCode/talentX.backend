using MediatR;
using TalentX.Application.Features.Invitations.DTOs;

namespace TalentX.Application.Features.Invitations.Commands.RespondToInvitationByToken;

public sealed record RespondToInvitationByTokenCommand(string Token, bool Accept) : IRequest<JobInvitationDto>;
