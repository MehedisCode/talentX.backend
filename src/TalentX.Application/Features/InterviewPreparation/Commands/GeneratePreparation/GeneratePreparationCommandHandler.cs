using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Commands.GeneratePreparation;

public sealed class GeneratePreparationCommandHandler
    : IRequestHandler<GeneratePreparationCommand, InterviewPreparationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IInterviewPreparationAiService _aiService;
    private readonly ILogger<GeneratePreparationCommandHandler> _logger;

    public GeneratePreparationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IInterviewPreparationAiService aiService,
        ILogger<GeneratePreparationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<InterviewPreparationDto> Handle(
        GeneratePreparationCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!;

        var invitation = await _unitOfWork.InterviewInvitations
            .GetByIdAsync(request.InvitationId, ct)
            ?? throw new NotFoundException("Interview invitation not found.", request.InvitationId.ToString());

        if (invitation.TalentUserId != userId)
            throw new ForbiddenException("You do not have access to this interview.");

        if (invitation.Status != InterviewInvitationStatus.Accepted)
            throw new BadRequestException("Interview preparation is only available for accepted interviews.");

        var existing = await _unitOfWork.InterviewPreparations
            .GetByInvitationIdAsync(request.InvitationId, ct);
        if (existing is not null)
            throw new ConflictException("Preparation already exists. Use regenerate instead.");

        var todayCount = await _unitOfWork.InterviewPreparations
            .CountTodayByTalentAsync(userId, ct);
        if (todayCount >= 5)
            throw new BadRequestException("Daily interview preparation limit reached (5/day).");

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
            _logger.LogError(ex, "AI preparation generation failed for invitation {Id}", request.InvitationId);
            throw new BadGatewayException("AI service is temporarily unavailable.");
        }

        var preparation = Domain.Entities.InterviewPreparation.Create(
            interviewInvitationId: invitation.Id,
            talentUserId: userId,
            jobId: job.Id,
            jobTitle: job.Title,
            companyName: job.CompanyName,
            interviewType: invitation.Type.ToString(),
            scheduledAt: invitation.ScheduledAt,
            technicalQuestions: aiResult.TechnicalQuestions,
            behavioralQuestions: aiResult.BehavioralQuestions,
            experienceBasedQuestions: aiResult.ExperienceBasedQuestions,
            roleSpecificQuestions: aiResult.RoleSpecificQuestions,
            keyTopicsToPrepare: aiResult.KeyTopicsToPrepare,
            suggestedRevisionAreas: aiResult.SuggestedRevisionAreas);

        await _unitOfWork.InterviewPreparations.AddAsync(preparation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(preparation);
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
