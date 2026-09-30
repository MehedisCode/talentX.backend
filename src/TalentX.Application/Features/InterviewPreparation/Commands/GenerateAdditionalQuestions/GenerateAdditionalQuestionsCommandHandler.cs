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

namespace TalentX.Application.Features.InterviewPreparation.Commands.GenerateAdditionalQuestions;

public sealed class GenerateAdditionalQuestionsCommandHandler
    : IRequestHandler<GenerateAdditionalQuestionsCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly IInterviewPreparationConfigProvider _configProvider;
    private readonly ILogger<GenerateAdditionalQuestionsCommandHandler> _logger;

    public GenerateAdditionalQuestionsCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        IInterviewPreparationConfigProvider configProvider,
        ILogger<GenerateAdditionalQuestionsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _configProvider = configProvider;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        GenerateAdditionalQuestionsCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;
        var config = await _configProvider.GetConfigAsync(ct);

        var preparation = await GetPreparationAsync(request.InvitationId, ct);

        InterviewPreparationAccessValidator.EnsureUserCanAccess(preparation, userId);
        InterviewPreparationAccessValidator.EnsurePreparationIsAccessible(preparation, config.AccessWindowAfterInterview);

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, ct);

        var context = CreateContext(preparation, talent?.Name);

        var result = await GenerateQuestionsAsync(context, request.InvitationId, ct);

        preparation.SetAdditionalQuestions(result.AdditionalQuestions);

        _unitOfWork.InterviewPreparations.Update(preparation);
        await _unitOfWork.SaveChangesAsync(ct);

        return InterviewPreparationMapper.ToDto(
            preparation, config.AccessWindowAfterInterview);
    }

    private async Task<InterviewPreparationEntity> GetPreparationAsync(
        Guid invitationId, CancellationToken ct)
    {
        return await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(invitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");
    }

    private static AdditionalQuestionsContext CreateContext(
        InterviewPreparationEntity preparation, string? talentName)
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
