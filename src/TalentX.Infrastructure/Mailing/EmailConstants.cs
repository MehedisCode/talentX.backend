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
    }

    public static class Subjects
    {
        public const string Otp = "Your TalentX Verification Code";
        public const string Credentials = "Your TalentX Login Credentials";
        public const string VerificationLink = "Verify Your Email — TalentX";
        public const string PasswordReset = "Reset Your TalentX Password";
        public const string AccountDeleted = "Your TalentX Account Has Been Deleted";
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
    }
}
