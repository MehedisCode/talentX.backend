using MediatR;

namespace TalentX.Application.Features.Interviews.Commands.DeclineInterviewInvitation;

public sealed record DeclineInterviewInvitationCommand(
    Guid InvitationId,
    string? Reason = null
) : IRequest;
