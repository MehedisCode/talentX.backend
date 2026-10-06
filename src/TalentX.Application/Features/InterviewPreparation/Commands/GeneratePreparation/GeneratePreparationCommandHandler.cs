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
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;

public sealed class GeneratePreparationCommandHandler
    : IRequestHandler<GeneratePreparationCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly IInterviewPreparationConfigProvider _configProvider;
    private readonly ILogger<GeneratePreparationCommandHandler> _logger;

    public GeneratePreparationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        IInterviewPreparationConfigProvider configProvider,
        ILogger<GeneratePreparationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        GeneratePreparationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var invitation = await _unitOfWork.InterviewInvitations
            .GetByIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview invitation not found.", request.InvitationId.ToString());

        InterviewPreparationAccessValidator.EnsureUserCanAccess(invitation, userId);

        if (invitation.Status != InterviewInvitationStatus.Accepted)
            throw new BadRequestException("Interview preparation is only available for accepted interviews.");

        var existing = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct);
        if (existing is not null)
            throw new ConflictException("Preparation already exists. Use regenerate instead.");

        var todayCount = await _unitOfWork.InterviewPreparations
            .CountInitialGenerationsTodayByTalentAsync(userId, ct);
        if (todayCount >= config.DailyGenerateLimit)
            throw new TooManyRequestsException(
                $"Daily interview preparation limit reached ({config.DailyGenerateLimit}/day).");

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

        var preparation = InterviewPreparationEntity.Create(
            new CreateInterviewPreparationPayload(
                InterviewInvitationId: invitation.Id,
                TalentUserId: userId,
                JobId: job.Id,
                JobTitle: job.Title,
                CompanyName: job.CompanyName,
                InterviewType: invitation.Type.ToString(),
                ScheduledAt: invitation.ScheduledAt,
                TechnicalQuestions: aiResult.TechnicalQuestions,
                BehavioralQuestions: aiResult.BehavioralQuestions,
                ExperienceBasedQuestions: aiResult.ExperienceBasedQuestions,
                RoleSpecificQuestions: aiResult.RoleSpecificQuestions,
                KeyTopicsToPrepare: aiResult.KeyTopicsToPrepare,
                SuggestedRevisionAreas: aiResult.SuggestedRevisionAreas));

        await _unitOfWork.InterviewPreparations.AddAsync(preparation, ct);
        await _unitOfWork.InterviewPreparations.AddGenerationAsync(
            InterviewPreparationGeneration.Create(new CreateGenerationPayload(
                preparation.Id, userId, InterviewPreparationGenerationKind.Initial)),
            ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Interview preparation {PreparationId} generated for talent {TalentUserId}: model {ModelName}, prompt {PromptVersion}, latency {LatencyMs}ms, tokens in {InputTokenCount} out {OutputTokenCount}",
            preparation.Id, userId,
            aiResult.Metadata?.ModelName ?? "unknown",
            aiResult.Metadata?.PromptVersion ?? "unknown",
            aiResult.Metadata?.LatencyMs ?? 0,
            aiResult.Metadata?.InputTokenCount ?? 0,
            aiResult.Metadata?.OutputTokenCount ?? 0);

        return InterviewPreparationMapper.ToDto(
            preparation, config.AccessWindowAfterInterview);
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
            _logger.LogError(ex, "AI preparation generation failed for invitation {Id}", invitationId);
            throw new BadGatewayException("AI service is temporarily unavailable.");
        }
    }
}
