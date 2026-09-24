namespace TalentX.Infrastructure.Mailing;

/// <summary>
/// Centralized constants for the email mailing infrastructure, including Razor template resource paths,
/// email subjects, and configuration defaults.
/// </summary>
public static class EmailConstants
{
    public static class Templates
    {
        public const string Otp = "TalentX.Infrastructure.Mailing.Templates.Emails.Otp";
        public const string Credentials = "TalentX.Infrastructure.Mailing.Templates.Emails.Credentials";
        public const string VerificationLink = "TalentX.Infrastructure.Mailing.Templates.Emails.VerificationLink";
        public const string InterviewInvitation = "TalentX.Infrastructure.Mailing.Templates.Emails.InterviewInvitation";
        public const string JobInvitation = "TalentX.Infrastructure.Mailing.Templates.Emails.JobInvitation";
        public const string InterviewConfirmation = "TalentX.Infrastructure.Mailing.Templates.Emails.InterviewConfirmation";
        public const string PasswordReset = "TalentX.Infrastructure.Mailing.Templates.Emails.PasswordReset";
        public const string AccountDeleted = "TalentX.Infrastructure.Mailing.Templates.Emails.AccountDeleted";
        public const string EmployerApproved = "TalentX.Infrastructure.Mailing.Templates.Emails.EmployerApproved";
        public const string EmployerRejected = "TalentX.Infrastructure.Mailing.Templates.Emails.EmployerRejected";
        public const string AccountStatusChanged = "TalentX.Infrastructure.Mailing.Templates.Emails.AccountStatusChanged";
        public const string JobAlert = "TalentX.Infrastructure.Mailing.Templates.Emails.JobAlert";
        public const string JobDeactivated = "TalentX.Infrastructure.Mailing.Templates.Emails.JobDeactivated";
        public const string JobReactivated = "TalentX.Infrastructure.Mailing.Templates.Emails.JobReactivated";
    }

    public static class Subjects
    {
        public const string Otp = "Your TalentX Verification Code";
        public const string Credentials = "Your TalentX Login Credentials";
        public const string VerificationLink = "Verify Your Email — TalentX";
        public const string PasswordReset = "Reset Your TalentX Password";
        public const string AccountDeleted = "Your TalentX Account Has Been Deleted";
        public const string EmployerApproved = "Your TalentX Employer Account Has Been Approved";
        public const string EmployerRejected = "Your TalentX Employer Account Verification Update";
        public const string JobAlert = "New Matching Job: {0} at {1} — TalentX";
        public const string JobDeactivated = "Your job \"{0}\" has been deactivated — TalentX";
        public const string JobReactivated = "Your job \"{0}\" is live again — TalentX";
    }

    public static class Defaults
    {
        public const string SenderName = "TalentX Support";
    }

    public static class Kinds
    {
        public const string Otp = "OTP";
        public const string Credentials = "CREDENTIALS";
        public const string VerificationLink = "VERIFICATION_LINK";
        public const string InterviewInvitation = "INTERVIEW_INVITATION";
        public const string InterviewConfirmation = "INTERVIEW_CONFIRMATION";
        public const string PasswordReset = "PASSWORD_RESET";
        public const string AccountDeleted = "ACCOUNT_DELETED";
        public const string EmployerApproved = "EMPLOYER_APPROVED";
        public const string EmployerRejected = "EMPLOYER_REJECTED";
        public const string AccountStatusChanged = "ACCOUNT_STATUS_CHANGED";
        public const string JobAlert = "JobAlert";
        public const string JobDeactivated = "JOB_DEACTIVATED";
        public const string JobReactivated = "JOB_REACTIVATED";
    }
}
