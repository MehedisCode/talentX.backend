using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation.Commands.EvaluateAnswers;

public sealed class EvaluateAnswersCommandHandler
    : IRequestHandler<EvaluateAnswersCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly InterviewPreparationSettings _settings;
    private readonly ILogger<EvaluateAnswersCommandHandler> _logger;

    public EvaluateAnswersCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        IOptions<InterviewPreparationSettings> settings,
        ILogger<EvaluateAnswersCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        EvaluateAnswersCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var preparation = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");

        EnsureUserCanAccess(preparation, userId);
        EnsurePreparationIsAccessible(preparation);

        var answers = await _unitOfWork.InterviewPreparations
            .GetAnswersByPreparationIdAsync(preparation.Id, ct);

        var answered = answers.Where(a => a.HasAnswer).ToList();
        if (answered.Count == 0)
            throw new BadRequestException(
                "Answer at least one question before requesting an AI evaluation.");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, ct);
        var totalCount = InterviewPreparationMapper.CountQuestions(preparation);

        var context = new AnswersEvaluationContext
        {
            JobTitle = preparation.JobTitle,
            CompanyName = preparation.CompanyName,
            TalentName = talent?.Name ?? "Candidate",
            InterviewType = preparation.InterviewType,
            TotalQuestionCount = totalCount,
            AnsweredQuestions = answered
                .Select(a => new AnsweredQuestion(a.Category, a.QuestionIndex, a.QuestionText, a.AnswerText))
                .ToList()
        };

        var result = await EvaluateWithAiAsync(context, request.InvitationId, ct);

        var payload = new CreateEvaluationPayload(
            InterviewPreparationId: preparation.Id,
            OverallScore: result.OverallScore,
            Summary: result.Summary,
            Strengths: result.Strengths,
            AreasToImprove: result.AreasToImprove,
            QuestionFeedback: result.QuestionFeedback,
            AnsweredCount: answered.Count,
            TotalCount: totalCount);

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

        return InterviewPreparationMapper.ToDto(
            preparation, _settings.AccessWindowAfterInterview, answers, evaluation);
    }

    private static void EnsureUserCanAccess(
        Domain.Entities.InterviewPreparation preparation, string userId)
    {
        if (preparation.TalentUserId != userId)
            throw new ForbiddenException("You do not have access to this interview preparation.");
    }

    private void EnsurePreparationIsAccessible(
        Domain.Entities.InterviewPreparation preparation)
    {
        if (!preparation.IsAccessible(_settings.AccessWindowAfterInterview))
            throw new ForbiddenException("Preparation is no longer accessible as the interview has already taken place.");
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
