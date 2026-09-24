using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Domain.Enums;
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
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("You must be signed in.");

        var existing = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");

        if (existing.TalentUserId != userId)
            throw new ForbiddenException("You do not have access to this interview preparation.");

        if (!existing.IsAccessible())
            throw new ForbiddenException("Preparation is no longer accessible as the interview has already taken place.");

        var talent = await _unitOfWork.Talents.GetByUserIdAsync(userId, ct);

        var context = new AdditionalQuestionsContext
        {
            JobTitle = existing.JobTitle,
            CompanyName = existing.CompanyName,
            TalentName = talent?.Name ?? "Candidate",
            ExistingTechnicalQuestions = existing.TechnicalQuestions,
            ExistingBehavioralQuestions = existing.BehavioralQuestions,
            ExistingRoleSpecificQuestions = existing.RoleSpecificQuestions,
            ExistingExperienceBasedQuestions = existing.ExperienceBasedQuestions
        };

        AdditionalQuestionsResult aiResult;
        try
        {
            aiResult = await _aiService.GenerateAdditionalQuestionsAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI additional questions generation failed for invitation {Id}", request.InvitationId);
            throw new BadGatewayException("AI service is temporarily unavailable.");
        }

        existing.SetAdditionalQuestions(aiResult.AdditionalQuestions);

        _unitOfWork.InterviewPreparations.Update(existing);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(existing);
    }

    private static InterviewPreparationDto MapToDto(Domain.Entities.InterviewPreparation preparation)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        return new InterviewPreparationDto
        {
            Id = preparation.Id,
            InterviewInvitationId = preparation.InterviewInvitationId,
            JobId = preparation.JobId,
            JobTitle = preparation.JobTitle,
            CompanyName = preparation.CompanyName,
            InterviewType = preparation.InterviewType,
            ScheduledAt = preparation.ScheduledAt,
            Status = preparation.Status.ToString(),
            TechnicalQuestions = Deserialize<List<PrepQuestion>>(preparation.TechnicalQuestions, options) ?? new(),
            BehavioralQuestions = Deserialize<List<PrepQuestion>>(preparation.BehavioralQuestions, options) ?? new(),
            ExperienceBasedQuestions = Deserialize<List<PrepQuestion>>(preparation.ExperienceBasedQuestions, options) ?? new(),
            RoleSpecificQuestions = Deserialize<List<PrepQuestion>>(preparation.RoleSpecificQuestions, options) ?? new(),
            KeyTopicsToPrepare = Deserialize<List<string>>(preparation.KeyTopicsToPrepare, options) ?? new(),
            SuggestedRevisionAreas = Deserialize<List<string>>(preparation.SuggestedRevisionAreas, options) ?? new(),
            AdditionalQuestions = string.IsNullOrEmpty(preparation.AdditionalQuestions) 
                ? null 
                : Deserialize<List<PrepQuestion>>(preparation.AdditionalQuestions, options),
            GenerationCount = preparation.GenerationCount,
            GeneratedAt = preparation.CreatedAt,
            IsAccessible = preparation.IsAccessible()
        };
    }

    private static T? Deserialize<T>(string json, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json, options); }
        catch { return default; }
    }
}
