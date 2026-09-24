using TalentX.Domain.Enums; // Assuming JobReportReason is here

namespace TalentX.Application.Features.JobReports;

public static class JobReportReasonLabels
{
    public static string ToLabel(JobReportReason reason)
    {
        return reason switch
        {
            JobReportReason.SuspectedScam => "Suspected scam or fraud",
            JobReportReason.MisleadingInformation => "Misleading job information",
            JobReportReason.InappropriateContent => "Inappropriate content",
            JobReportReason.IncorrectSalaryInformation => "Incorrect salary or compensation information",
            JobReportReason.EmployerImpersonation => "Employer impersonation",
            JobReportReason.DuplicateJob => "Duplicate job posting",
            JobReportReason.DiscriminatoryContent => "Discriminatory content",
            JobReportReason.Other => "Other issue",
            _ => "Other issue"
        };
    }
}
