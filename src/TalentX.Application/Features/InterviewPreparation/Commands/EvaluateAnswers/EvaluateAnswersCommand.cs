using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Commands.EvaluateAnswers;

public sealed record EvaluateAnswersCommand(Guid InvitationId) : IRequest<InterviewPreparationDto>;
