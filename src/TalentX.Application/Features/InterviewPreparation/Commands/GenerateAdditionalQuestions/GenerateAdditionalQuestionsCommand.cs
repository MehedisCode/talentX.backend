using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;

public sealed record GenerateAdditionalQuestionsCommand(Guid InvitationId) : IRequest<InterviewPreparationDto>;
