using MediatR;

namespace TalentX.Application.Features.Interviews.Commands.AcceptInterviewInvitation;

public sealed record AcceptInterviewInvitationCommand(
    Guid InvitationId,
    Guid? SelectedTimeslotId = null
) : IRequest;
