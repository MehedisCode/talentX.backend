using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Commands.RegeneratePreparation;

public sealed class RegeneratePreparationCommandHandler
    : IRequestHandler<RegeneratePreparationCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly ILogger<RegeneratePreparationCommandHandler> _logger;

    public RegeneratePreparationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        ILogger<RegeneratePreparationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        RegeneratePreparationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var existing = await GetPreparationAsync(request.InvitationId, ct);

        EnsureUserCanAccess(existing, userId);
        EnsurePreparationIsAccessible(existing);

        if (existing.GenerationCount >= 3)
            throw new BadRequestException("Maximum regeneration limit reached (3 times).");

        var todayCount = await _unitOfWork.InterviewPreparations
            .CountTodayByTalentAsync(userId, ct);
        if (todayCount >= 10)
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
            job, talent, invitation, workExperiences, educations);

        var aiResult = await GenerateWithAiAsync(context, request.InvitationId, ct);

        existing.Regenerate(
            aiResult.TechnicalQuestions,
            aiResult.BehavioralQuestions,
            aiResult.ExperienceBasedQuestions,
            aiResult.RoleSpecificQuestions,
            aiResult.KeyTopicsToPrepare,
            aiResult.SuggestedRevisionAreas
        );

        _unitOfWork.InterviewPreparations.Update(existing);
        await _unitOfWork.SaveChangesAsync(ct);

        return InterviewPreparationMapper.ToDto(existing);
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

    private static void EnsurePreparationIsAccessible(
        Domain.Entities.InterviewPreparation preparation)
    {
        if (!preparation.IsAccessible())
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
