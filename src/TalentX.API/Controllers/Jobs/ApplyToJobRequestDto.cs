using Microsoft.AspNetCore.Http;

namespace TalentX.API.Controllers;

public sealed record ApplyToJobRequestDto(
    string ApplicantName,
    string ApplicantEmail,
    IFormFile? ResumeFile,
    string CoverLetter,
    decimal ExpectedSalary,
    decimal TotalYearsExperience,
    string? Location,
    string? ExtractedTechnicalSkills = null,
    string? ExtractedSoftSkills = null,
    string? ExtractedToolsAndPlatforms = null,
    string? ExtractedWorkExperience = null,
    string? ExtractedEducation = null,
    string? ExtractedProjects = null,
    string? ExtractedProfile = null);
