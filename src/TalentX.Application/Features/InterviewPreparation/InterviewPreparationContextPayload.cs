using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation;

public sealed record InterviewPreparationContextPayload(
    Job Job,
    Talent Talent,
    InterviewInvitation Invitation,
    IReadOnlyList<WorkExperience> WorkExperiences,
    IReadOnlyList<Education> Educations);
