using System.Text.Json;
using TalentX.Application.Features.InterviewPreparation.DTOs;

namespace TalentX.Application.Features.InterviewPreparation.Mappers;

/// <summary>
/// Single place where <see cref="InterviewPreparation"/> entities become DTOs.
/// The one fallback policy for stored question JSON lives in
/// <see cref="DeserializeList{T}"/>: blank or corrupt JSON degrades to an empty
/// list instead of failing the request (silent by design — one bad field must
/// not break the whole response, including list queries).
/// </summary>
public static class InterviewPreparationMapper
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new() { PropertyNameCaseInsensitive = true };

    public static InterviewPreparationDto ToDto(Domain.Entities.InterviewPreparation preparation)
    {
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
            TechnicalQuestions = DeserializeList<PrepQuestion>(preparation.TechnicalQuestions),
            BehavioralQuestions = DeserializeList<PrepQuestion>(preparation.BehavioralQuestions),
            ExperienceBasedQuestions = DeserializeList<PrepQuestion>(preparation.ExperienceBasedQuestions),
            RoleSpecificQuestions = DeserializeList<PrepQuestion>(preparation.RoleSpecificQuestions),
            KeyTopicsToPrepare = DeserializeList<string>(preparation.KeyTopicsToPrepare),
            SuggestedRevisionAreas = DeserializeList<string>(preparation.SuggestedRevisionAreas),
            AdditionalQuestions = DeserializeList<PrepQuestion>(preparation.AdditionalQuestions),
            GenerationCount = preparation.GenerationCount,
            GeneratedAt = preparation.CreatedAt,
            IsAccessible = preparation.IsAccessible()
        };
    }

    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
