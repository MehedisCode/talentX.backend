using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;

public sealed record RegeneratePreparationCommand(Guid InvitationId) : IRequest<InterviewPreparationDto>;
