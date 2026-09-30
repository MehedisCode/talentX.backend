using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Commands.SaveAnswer;

public sealed record SaveAnswerCommand(
    Guid InvitationId,
    string Category,
    int QuestionIndex,
    string Question,
    string Answer,
    bool Practiced) : IRequest<InterviewPreparationDto>;
