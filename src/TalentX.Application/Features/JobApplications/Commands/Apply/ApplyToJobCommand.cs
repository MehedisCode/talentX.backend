using MediatR;

namespace TalentX.Application.Features.JobApplications.Commands.Apply;

public record ApplyToJobCommand(
    Guid JobId,
    string ApplicantName,
    string ApplicantEmail,
    string ResumeStorageKey,
    string CoverLetter,
    decimal ExpectedSalary,
    decimal TotalYearsExperience,
    string? Location
) : IRequest<ApplyToJobResponse>;