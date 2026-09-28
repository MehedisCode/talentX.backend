using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation;

/// <summary>
/// Source entities used to build an <see cref="DTOs.InterviewPreparationContext"/> —
/// groups the argument list shared by the generate/regenerate handlers.
/// </summary>
public sealed record InterviewPreparationContextPayload(
    Job Job,
    Talent Talent,
    InterviewInvitation Invitation,
    IReadOnlyList<WorkExperience> WorkExperiences,
    IReadOnlyList<Education> Educations);
