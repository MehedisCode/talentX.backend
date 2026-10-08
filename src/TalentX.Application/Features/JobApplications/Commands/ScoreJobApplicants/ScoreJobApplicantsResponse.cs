namespace TalentX.Application.Features.JobApplications.Commands.ScoreJobApplicants;

public sealed record ScoreJobApplicantsResponse(
    Guid JobId,
    int ScoredCount,
    int ReusedCount = 0);
