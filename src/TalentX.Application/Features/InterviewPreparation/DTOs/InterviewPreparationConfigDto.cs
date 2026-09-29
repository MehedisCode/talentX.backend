namespace TalentX.Application.Features.InterviewPreparation.DTOs;

public sealed record InterviewPreparationConfigDto(
    Guid Id,
    int DailyGenerateLimit,
    int DailyGenerationLimit,
    int MaxRegenerationCount,
    int AccessWindowAfterInterviewMinutes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
