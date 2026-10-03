using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Helpers;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;
using InterviewPreparationEntity = TalentX.Domain.Entities.InterviewPreparation;

namespace TalentX.Application.Features.InterviewPreparation.Commands.SaveAnswer;

public sealed class SaveAnswerCommandHandler
    : IRequestHandler<SaveAnswerCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationConfigProvider _configProvider;

    public SaveAnswerCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationConfigProvider configProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _configProvider = configProvider;
    }

    public async Task<InterviewPreparationDto> Handle(
        SaveAnswerCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var preparation = await GetPreparationAsync(request.InvitationId, ct);

        InterviewPreparationAccessValidator.EnsureUserCanAccess(preparation, userId);
        InterviewPreparationAccessValidator.EnsurePreparationIsAccessible(
            preparation, config.AccessWindowAfterInterview);

        var question = InterviewPreparationMapper.FindQuestion(
                preparation, request.Category, request.QuestionIndex)
            ?? throw new BadRequestException("Question not found in this preparation.");

        var answerText = request.Answer?.Trim() ?? string.Empty;

        var existing = await _unitOfWork.InterviewPreparations.GetAnswerAsync(
            preparation.Id, request.Category, request.QuestionIndex, ct);

        bool evaluationInvalidated;

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

        return await InterviewPreparationDtoLoader.LoadAsync(
            _unitOfWork, preparation, config.AccessWindowAfterInterview, ct);
    }

    private async Task<InterviewPreparationEntity> GetPreparationAsync(
        Guid invitationId, CancellationToken ct)
    {
        return await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(invitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");
    }
}
