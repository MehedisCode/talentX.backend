using MediatR;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation.Commands.SaveAnswer;

public sealed class SaveAnswerCommandHandler
    : IRequestHandler<SaveAnswerCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly InterviewPreparationSettings _settings;

    public SaveAnswerCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IOptions<InterviewPreparationSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _settings = settings.Value;
    }

    public async Task<InterviewPreparationDto> Handle(
        SaveAnswerCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var preparation = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");

        EnsureUserCanAccess(preparation, userId);
        EnsurePreparationIsAccessible(preparation);

        var question = InterviewPreparationMapper.FindQuestion(
                preparation, request.Category, request.QuestionIndex)
            ?? throw new BadRequestException("Question not found in this preparation.");

        var answerText = request.Answer?.Trim() ?? string.Empty;

        var existing = await _unitOfWork.InterviewPreparations.GetAnswerAsync(
            preparation.Id, request.Category, request.QuestionIndex, ct);

        var evaluationInvalidated = false;

        if (existing is null)
        {
            await _unitOfWork.InterviewPreparations.AddAnswerAsync(
                PreparationAnswer.Create(new CreatePreparationAnswerPayload(
                    preparation.Id,
                    request.Category,
                    request.QuestionIndex,
                    question.Question,
                    answerText,
                    request.Practiced)), ct);

            evaluationInvalidated = answerText.Length > 0;
        }
        else
        {
            var textChanged = existing.AnswerText != answerText;

            existing.Update(new UpdatePreparationAnswerPayload(
                question.Question, answerText, request.Practiced));
            _unitOfWork.InterviewPreparations.UpdateAnswer(existing);

            evaluationInvalidated = textChanged;
        }

        if (evaluationInvalidated)
        {
            await _unitOfWork.InterviewPreparations
                .RemoveEvaluationByPreparationIdAsync(preparation.Id, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await BuildDtoAsync(preparation, ct);
    }

    private async Task<InterviewPreparationDto> BuildDtoAsync(
        Domain.Entities.InterviewPreparation preparation, CancellationToken ct)
    {
        var answers = await _unitOfWork.InterviewPreparations
            .GetAnswersByPreparationIdAsync(preparation.Id, ct);
        var evaluation = await _unitOfWork.InterviewPreparations
            .GetEvaluationByPreparationIdAsync(preparation.Id, ct);

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
}
