using TalentX.Application.Features.InterviewPreparation.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.InterviewPreparation;

public static class InterviewPreparationContextFactory
{
    public static InterviewPreparationContext CreateContext(
        InterviewPreparationContextPayload payload)
    {
        var job = payload.Job;
        var talent = payload.Talent;
        var invitation = payload.Invitation;
        var workExperiences = payload.WorkExperiences;
        var educations = payload.Educations;

        return new InterviewPreparationContext
        {
            JobTitle = job.Title,
            CompanyName = job.CompanyName,
            JobDescription = job.Description,
            Responsibilities = job.Responsibilities,
            Requirements = job.Requirements,
            RequiredSkills = job.RequiredSkills,
            ExperienceLevel = job.ExperienceLevel.ToString(),
            EmploymentType = job.EmploymentType.ToString(),
            WorkArrangement = job.WorkArrangement.ToString(),
            TalentName = talent.Name,
            Tagline = talent.Tagline,
            ShortBio = talent.ShortBio,
            TotalYearsExperience = talent.TotalYearsExperience,
            TechnicalSkills = talent.TechnicalSkills,
            SoftSkills = talent.SoftSkills,
            ToolsAndPlatforms = talent.ToolsAndPlatforms,
            WorkExperiences = workExperiences.Select(w =>
                new WorkExperienceSnapshot(w.CompanyName, w.JobTitle, w.StartDate ?? "", w.EndDate, w.IsCurrent)).ToList(),
            Educations = educations.Select(e =>
                new EducationSnapshot(e.Institution, e.Degree, e.StartDate ?? "", e.EndDate)).ToList(),
            InterviewType = invitation.Type.ToString(),
            ScheduledAt = invitation.ScheduledAt
        };
    }
}
