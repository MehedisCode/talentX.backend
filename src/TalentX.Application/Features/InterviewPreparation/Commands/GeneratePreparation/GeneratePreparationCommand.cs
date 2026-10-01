using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;

public sealed record GeneratePreparationCommand(Guid InvitationId) : IRequest<InterviewPreparationDto>;
