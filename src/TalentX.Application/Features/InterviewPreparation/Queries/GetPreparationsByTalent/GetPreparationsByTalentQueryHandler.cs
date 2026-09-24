using System.Text.Json;
using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.InterviewPreparation.Queries.GetPreparationsByTalent;

public sealed class GetPreparationsByTalentQueryHandler
    : IRequestHandler<GetPreparationsByTalentQuery, IReadOnlyList<InterviewPreparationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPreparationsByTalentQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InterviewPreparationDto>> Handle(
        GetPreparationsByTalentQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("You must be signed in.");

        var preparations = await _unitOfWork.InterviewPreparations
            .GetByTalentUserIdAsync(userId, ct);

        return preparations.Select(MapToDto).ToList();
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
