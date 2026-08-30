using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.JobApplications.Commands.ApplyToJob;

public record ApplyToJobCommand(
    Guid JobId,
    string ApplicantName,
    string ApplicantEmail,
    string ResumeStorageKey,
    string? CoverLetter,
    decimal? ExpectedSalary,
    decimal? TotalYearsExperience,
    string? Location) : IRequest<ApplicationSubmittedDto>;
