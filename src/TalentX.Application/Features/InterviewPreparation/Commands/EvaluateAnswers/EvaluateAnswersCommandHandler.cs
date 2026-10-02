using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Helpers;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Features.InterviewPreparation.Commands.EvaluateAnswers;

public sealed class EvaluateAnswersCommandHandler
    : IRequestHandler<EvaluateAnswersCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly IInterviewPreparationConfigProvider _configProvider;
    private readonly ILogger<EvaluateAnswersCommandHandler> _logger;

    public EvaluateAnswersCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        IInterviewPreparationConfigProvider configProvider,
        ILogger<EvaluateAnswersCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        EvaluateAnswersCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var preparation = await GetPreparationAsync(request.InvitationId, ct);

        InterviewPreparationAccessValidator.EnsureUserCanAccess(preparation, userId);
        InterviewPreparationAccessValidator.EnsurePreparationIsAccessible(
            preparation, config.AccessWindowAfterInterview);

        var answers = await _unitOfWork.InterviewPreparations
            .GetAnswersByPreparationIdAsync(preparation.Id, ct);

        var answered = answers.Where(a => a.HasAnswer).ToList();
        if (answered.Count == 0)
            throw new BadRequestException(
                "Answer at least one question before requesting an AI evaluation.");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, ct);
        var context = AnswersEvaluationContextFactory.CreateContext(
            preparation, talent?.Name, answered);

        var result = await EvaluateWithAiAsync(context, request.InvitationId, ct);

        var evaluation = await UpsertEvaluationAsync(preparation, context, result, ct);

        return InterviewPreparationMapper.ToDto(
            preparation, config.AccessWindowAfterInterview, answers, evaluation);
    }

    private async Task<InterviewPreparationEntity> GetPreparationAsync(
        Guid invitationId, CancellationToken ct)
    {
        return await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(invitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");
    }

    private async Task<InterviewPreparationEvaluation> UpsertEvaluationAsync(
        InterviewPreparationEntity preparation,
        AnswersEvaluationContext context,
        AnswersEvaluationResult result,
        CancellationToken ct)
    {
        var payload = new CreateEvaluationPayload(
            InterviewPreparationId: preparation.Id,
            OverallScore: result.OverallScore,
            Summary: result.Summary,
            Strengths: result.Strengths,
            AreasToImprove: result.AreasToImprove,
            QuestionFeedback: result.QuestionFeedback,
            AnsweredCount: context.AnsweredQuestions.Count,
            TotalCount: context.TotalQuestionCount);

        var evaluation = await _unitOfWork.InterviewPreparations
            .GetEvaluationByPreparationIdAsync(preparation.Id, ct);

        if (evaluation is null)
        {
            evaluation = InterviewPreparationEvaluation.Create(payload);
            await _unitOfWork.InterviewPreparations.AddEvaluationAsync(evaluation, ct);
        }
        else
        {
            evaluation.Replace(payload);
            _unitOfWork.InterviewPreparations.UpdateEvaluation(evaluation);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return evaluation;
    }

    private async Task<AnswersEvaluationResult> EvaluateWithAiAsync(
        AnswersEvaluationContext context, Guid invitationId, CancellationToken ct)
    {
        try
        {
            return await _aiService.EvaluateAnswersAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI answer evaluation failed for invitation {Id}", invitationId);
            throw new BadGatewayException("AI service is temporarily unavailable.");
        }
    }
}
