using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Features.InterviewPreparation.Mappers;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;

public sealed class GenerateAdditionalQuestionsCommandHandler
    : IRequestHandler<GenerateAdditionalQuestionsCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly ILogger<GenerateAdditionalQuestionsCommandHandler> _logger;

    public GenerateAdditionalQuestionsCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        ILogger<GenerateAdditionalQuestionsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        GenerateAdditionalQuestionsCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var preparation = await GetPreparationAsync(request.InvitationId, ct);

        EnsureUserCanAccess(preparation, userId);
        EnsurePreparationIsAccessible(preparation);

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, ct);

        var context = CreateContext(preparation, talent?.Name);

        var result = await GenerateQuestionsAsync(context, request.InvitationId, ct);

        preparation.SetAdditionalQuestions(result.AdditionalQuestions);

        _unitOfWork.InterviewPreparations.Update(preparation);
        await _unitOfWork.SaveChangesAsync(ct);

        return InterviewPreparationMapper.ToDto(preparation);
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

    private static AdditionalQuestionsContext CreateContext(
        Domain.Entities.InterviewPreparation preparation, string? talentName)
    {
        return new AdditionalQuestionsContext
        {
            JobTitle = preparation.JobTitle,
            CompanyName = preparation.CompanyName,
            TalentName = talentName ?? "Candidate",
            ExistingTechnicalQuestions = preparation.TechnicalQuestions,
            ExistingBehavioralQuestions = preparation.BehavioralQuestions,
            ExistingRoleSpecificQuestions = preparation.RoleSpecificQuestions,
            ExistingExperienceBasedQuestions = preparation.ExperienceBasedQuestions
        };
    }

    private async Task<AdditionalQuestionsResult> GenerateQuestionsAsync(
        AdditionalQuestionsContext context, Guid invitationId, CancellationToken ct)
    {
        try
        {
            return await _aiService.GenerateAdditionalQuestionsAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI additional questions generation failed for invitation {Id}", invitationId);
            throw new BadGatewayException("AI service is temporarily unavailable.");
        }
    }
}
