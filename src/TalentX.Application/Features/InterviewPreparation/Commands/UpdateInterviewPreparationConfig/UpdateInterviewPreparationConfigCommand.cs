using MediatR;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Commands.UpdateInterviewPreparationConfig;

public sealed record UpdateInterviewPreparationConfigCommand(
    int DailyGenerateLimit,
    int DailyGenerationLimit,
    int MaxRegenerationCount,
    int AccessWindowAfterInterviewMinutes
) : IRequest<InterviewPreparationConfigDto>;
