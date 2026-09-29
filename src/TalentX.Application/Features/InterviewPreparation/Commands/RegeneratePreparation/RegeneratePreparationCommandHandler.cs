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

namespace TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;

public sealed class RegeneratePreparationCommandHandler
    : IRequestHandler<RegeneratePreparationCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly IInterviewPreparationConfigProvider _configProvider;
    private readonly ILogger<RegeneratePreparationCommandHandler> _logger;

    public RegeneratePreparationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        IInterviewPreparationConfigProvider configProvider,
        ILogger<RegeneratePreparationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        RegeneratePreparationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var existing = await GetPreparationAsync(request.InvitationId, ct);

        InterviewPreparationAccessValidator.EnsureUserCanAccess(existing, userId);
        InterviewPreparationAccessValidator.EnsurePreparationIsAccessible(existing, config.AccessWindowAfterInterview);

        if (existing.GenerationCount >= config.MaxRegenerationCount)
            throw new TooManyRequestsException(
                $"Maximum regeneration limit reached ({config.MaxRegenerationCount} times).");

        var todayCount = await _unitOfWork.InterviewPreparations
            .CountTodayByTalentAsync(userId, ct);
        if (todayCount >= config.DailyGenerationLimit)
            throw new TooManyRequestsException("Daily interview preparation generation limit reached.");

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
        await _unitOfWork.SaveChangesAsync(ct);

        return InterviewPreparationMapper.ToDto(
            existing, config.AccessWindowAfterInterview);
    }

    private async Task<InterviewPreparationEntity> GetPreparationAsync(
        Guid invitationId, CancellationToken ct)
    {
        return await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(invitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");
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
