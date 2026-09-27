using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Domain.Enums;
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

        var existing = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview preparation not found.");

        if (existing.TalentUserId != userId)
            throw new ForbiddenException("You do not have access to this interview preparation.");

        if (!existing.IsAccessible())
            throw new ForbiddenException("Preparation is no longer accessible as the interview has already taken place.");

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

        var context = new InterviewPreparationContext
        {
            JobTitle = job.Title,
            CompanyName = job.CompanyName,
            JobDescription = job.Description,
            Responsibilities = job.Responsibilities,
            Requirements = job.Requirements,
            RequiredSkills = job.RequiredSkills,
            ExperienceLevel = job.ExperienceLevel.ToString(),
            EmploymentType = job.EmploymentType.ToString(),
            WorkArrangement = job.WorkArrangement.ToString(),
            TalentName = talent.Name,
            Tagline = talent.Tagline,
            ShortBio = talent.ShortBio,
            TotalYearsExperience = talent.TotalYearsExperience,
            TechnicalSkills = talent.TechnicalSkills,
            SoftSkills = talent.SoftSkills,
            ToolsAndPlatforms = talent.ToolsAndPlatforms,
            WorkExperiences = workExperiences.Select(w =>
                new WorkExperienceSnapshot(w.CompanyName, w.JobTitle, w.StartDate ?? "", w.EndDate, w.IsCurrent)).ToList(),
            Educations = educations.Select(e =>
                new EducationSnapshot(e.Institution, e.Degree, e.StartDate ?? "", e.EndDate)).ToList(),
            InterviewType = invitation.Type.ToString(),
            ScheduledAt = invitation.ScheduledAt
        };

        InterviewPreparationResult aiResult;
        try
        {
            aiResult = await _aiService.GeneratePreparationAsync(context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI preparation regeneration failed for invitation {Id}", request.InvitationId);
            throw new BadGatewayException("AI service is temporarily unavailable.");
        }

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
