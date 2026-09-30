using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;

public sealed class RegeneratePreparationCommandHandler
    : IRequestHandler<RegeneratePreparationCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly InterviewPreparationSettings _settings;
    private readonly ILogger<RegeneratePreparationCommandHandler> _logger;

    public RegeneratePreparationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        IOptions<InterviewPreparationSettings> settings,
        ILogger<RegeneratePreparationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        RegeneratePreparationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var existing = await GetPreparationAsync(request.InvitationId, ct);

        EnsureUserCanAccess(existing, userId);
        EnsurePreparationIsAccessible(existing);

        if (existing.GenerationCount >= _settings.MaxRegenerationCount)
            throw new BadRequestException(
                $"Maximum regeneration limit reached ({_settings.MaxRegenerationCount} times).");

        var todayCount = await _unitOfWork.InterviewPreparations
            .CountTodayByTalentAsync(userId, ct);
        if (todayCount >= _settings.DailyGenerationLimit)
            throw new BadRequestException("Daily interview preparation generation limit reached.");

        var invitation = await _unitOfWork.InterviewInvitations
            .GetByIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview invitation not found.");

        var job = await _unitOfWork.Jobs.GetByIdAsync(invitation.JobId, ct)
            ?? throw new NotFoundException("Job not found.");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Talent profile not found.");

        var workExperiences = await _unitOfWork.WorkExperiences
            .GetByUserIdAsync(userId, ct);

        var educations = await _unitOfWork.Educations
            .GetByUserIdAsync(userId, ct);

        var context = InterviewPreparationContextFactory.CreateContext(
            new InterviewPreparationContextPayload(
                job, talent, invitation, workExperiences, educations));

        var aiResult = await GenerateWithAiAsync(context, request.InvitationId, ct);

        existing.Regenerate(
            new RegenerateInterviewPreparationPayload(
                TechnicalQuestions: aiResult.TechnicalQuestions,
                BehavioralQuestions: aiResult.BehavioralQuestions,
                ExperienceBasedQuestions: aiResult.ExperienceBasedQuestions,
                RoleSpecificQuestions: aiResult.RoleSpecificQuestions,
                KeyTopicsToPrepare: aiResult.KeyTopicsToPrepare,
                SuggestedRevisionAreas: aiResult.SuggestedRevisionAreas)
        );

        _unitOfWork.InterviewPreparations.Update(existing);

        // Regenerated questions replace the originals, so saved answers and any prior
        // evaluation no longer reference valid questions.
        await _unitOfWork.InterviewPreparations
            .RemoveAnswersByPreparationIdAsync(existing.Id, ct);
        await _unitOfWork.InterviewPreparations
            .RemoveEvaluationByPreparationIdAsync(existing.Id, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return InterviewPreparationMapper.ToDto(
            existing, _settings.AccessWindowAfterInterview);
    }

    private async Task<Domain.Entities.InterviewPreparation> GetPreparationAsync(
        Guid invitationId, CancellationToken ct)
    {
        return await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(invitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");
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

    private async Task<InterviewPreparationResult> GenerateWithAiAsync(
        InterviewPreparationContext context, Guid invitationId, CancellationToken ct)
    {
        try
        {
            return await _aiService.GeneratePreparationAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI preparation regeneration failed for invitation {Id}", invitationId);
            throw new BadGatewayException("AI service is temporarily unavailable.");
        }
    }
}
