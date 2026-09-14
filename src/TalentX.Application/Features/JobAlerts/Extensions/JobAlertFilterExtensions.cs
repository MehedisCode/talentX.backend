using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.JobAlerts.Extensions;

public static class JobAlertFilterExtensions
{
    public static IQueryable<Job> ApplyAlertCriteria(this IQueryable<Job> query, JobAlert alert)
    {
        var currentDate = DateTime.UtcNow.Date;
        query = query.Where(job => !job.IsDeleted && job.Status == JobStatus.Published && job.ExpireAt.Date >= currentDate);

        if (!string.IsNullOrWhiteSpace(alert.Title))
        {
            var titleLower = alert.Title.Trim().ToLower();
            query = query.Where(job => job.Title.ToLower().Contains(titleLower));
        }

        if (!string.IsNullOrWhiteSpace(alert.Location))
        {
            var locationLower = alert.Location.Trim().ToLower();
            query = query.Where(job =>
                (job.Location != null && job.Location.ToLower().Contains(locationLower)) ||
                (job.City != null && job.City.ToLower().Contains(locationLower)) ||
                (job.Country != null && job.Country.ToLower().Contains(locationLower)));
        }

        if (alert.EmploymentType.HasValue)
        {
            query = query.Where(job => job.EmploymentType == alert.EmploymentType.Value);
        }

        if (alert.ExperienceLevel.HasValue)
        {
            query = query.Where(job => job.ExperienceLevel == alert.ExperienceLevel.Value);
        }

        if (alert.WorkArrangement.HasValue)
        {
            query = query.Where(job => job.WorkArrangement == alert.WorkArrangement.Value);
        }

        return query;
    }

    public static IEnumerable<Job> ApplyAlertCriteria(this IEnumerable<Job> jobs, JobAlert alert)
    {
        return jobs.AsQueryable().ApplyAlertCriteria(alert).AsEnumerable();
    }

    public static bool MatchesJob(this JobAlert alert, Job job)
    {
        return new[] { job }.ApplyAlertCriteria(alert).Any();
    }
}
