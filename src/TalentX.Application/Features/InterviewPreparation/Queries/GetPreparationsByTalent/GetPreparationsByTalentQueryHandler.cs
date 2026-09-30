using MediatR;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;

public sealed class GetPreparationsByTalentQueryHandler
    : IRequestHandler<GetPreparationsByTalentQuery, IReadOnlyList<InterviewPreparationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly InterviewPreparationSettings _settings;

    public GetPreparationsByTalentQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IOptions<InterviewPreparationSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<InterviewPreparationDto>> Handle(
        GetPreparationsByTalentQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var preparations = await _unitOfWork.InterviewPreparations
            .GetByTalentUserIdAsync(userId, ct);

        var accessWindow = _settings.AccessWindowAfterInterview;

        if (preparations.Count == 0)
            return [];

        var preparationIds = preparations.Select(p => p.Id).ToList();

        var answers = await _unitOfWork.InterviewPreparations
            .GetAnswersByPreparationIdsAsync(preparationIds, ct);
        var evaluations = await _unitOfWork.InterviewPreparations
            .GetEvaluationsByPreparationIdsAsync(preparationIds, ct);

        var answersByPreparation = answers
            .GroupBy(a => a.InterviewPreparationId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Entities.PreparationAnswer>)g.ToList());
        var evaluationByPreparation = evaluations
            .ToDictionary(e => e.InterviewPreparationId);

        return preparations
            .Select(p => InterviewPreparationMapper.ToDto(
                p,
                accessWindow,
                answersByPreparation.GetValueOrDefault(p.Id),
                evaluationByPreparation.GetValueOrDefault(p.Id)))
            .ToList();
    }
}
