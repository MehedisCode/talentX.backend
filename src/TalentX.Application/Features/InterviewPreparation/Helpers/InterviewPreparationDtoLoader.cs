using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Features.InterviewPreparation.Helpers;

public static class InterviewPreparationDtoLoader
{
    public static async Task<InterviewPreparationDto> LoadAsync(
        IUnitOfWork unitOfWork,
        InterviewPreparationEntity preparation,
        TimeSpan accessWindow,
        CancellationToken ct = default)
    {
        var answers = await unitOfWork.InterviewPreparations
            .GetAnswersByPreparationIdAsync(preparation.Id, ct);
        var evaluation = await unitOfWork.InterviewPreparations
            .GetEvaluationByPreparationIdAsync(preparation.Id, ct);

        return InterviewPreparationMapper.ToDto(preparation, accessWindow, answers, evaluation);
    }
}
