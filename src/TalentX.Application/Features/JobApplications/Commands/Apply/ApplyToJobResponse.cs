namespace TalentX.Application.Features.JobApplications.Commands.Apply;

public record ApplyToJobResponse(
    Guid ApplicationId,
    Guid JobId,
    bool VerificationEmailResent = false
);
