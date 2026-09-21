using MediatR;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.JobApplications.Commands.Apply;

public record ApplyToJobCommand(
    Guid JobId,
    string ApplicantName,
    string ApplicantEmail,
    IUploadedFile ResumeFile,
    string CoverLetter,
    decimal ExpectedSalary,
    decimal TotalYearsExperience,
    string? Location,
    string? ExtractedTechnicalSkillsJson = null,
    string? ExtractedSoftSkillsJson = null,
    string? ExtractedToolsAndPlatformsJson = null,
    string? ExtractedWorkExperienceJson = null,
    string? ExtractedEducationJson = null
) : IRequest<ApplyToJobResponse>;