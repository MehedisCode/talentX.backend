using TalentX.Application.Common.Models;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Service contract for sending notification and verification emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an OTP email to the recipient for email verification.
    /// </summary>
    /// <param name="toEmail">Destination email address.</param>
    /// <param name="recipientName">Name of the recipient.</param>
    /// <param name="otpCode">The 6-digit OTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendOtpEmailAsync(
        string toEmail,
        string recipientName,
        string otpCode,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends an email with temporary login credentials to the recipient.
    /// </summary>
    Task SendCredentialsEmailAsync(
        string toEmail,
        string recipientName,
        string temporaryPassword,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends an email with a verification link to confirm the recipient's email address.
    /// </summary>
    Task SendVerificationLinkEmailAsync(
        string toEmail,
        string recipientName,
        string verificationUrl,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Renders the HTML body for a new-interview-invitation notification email.
    /// </summary>
    Task<string> RenderInterviewInvitationEmailAsync(
        string recipientName,
        string jobTitle,
        string companyName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Renders the HTML body for an interview-details-confirmed email.
    /// </summary>
    Task<string> RenderInterviewConfirmationEmailAsync(
        string recipientName,
        string jobTitle,
        string companyName,
        string interviewType,
        string locationOrLink,
        string scheduledTimeOrDeadline,
        string? instructions,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Synchronously dispatches a pre-rendered email via SMTP, throwing on failure so the
    /// caller (the email outbox background worker) can record the failure and retry.
    /// </summary>
    Task DispatchDirectAsync(
        string toEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends a job invitation notification email to a candidate.
    /// </summary>
    Task SendJobInvitationEmailAsync(
        string toEmail,
        string recipientName,
        string jobTitle,
        string companyName,
        string jobUrl,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends an email with a password reset link to the recipient.
    /// </summary>
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Renders the HTML body for an account activated/deactivated notification email. The
    /// deactivation copy is tailored to the recipient's role (e.g. an Employer is told they can't
    /// post jobs, a Talent that they can't apply to jobs).
    /// </summary>
    Task<string> RenderAccountStatusChangedEmailAsync(
        string recipientName,
        AccountStatus newStatus,
        UserRole role,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Renders the HTML body for a job alert notification email.
    /// </summary>
    Task<string> RenderJobAlertEmailAsync(
        RenderJobAlertEmailParameters parameters,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Renders the HTML body telling an employer that an admin deactivated their job, including
    /// the reason, the admin's explanation, and a link to edit the job so it can be resubmitted.
    /// </summary>
    Task<string> RenderJobDeactivatedEmailAsync(
        RenderJobDeactivatedEmailParameters parameters,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Renders the HTML body telling an employer that their job is published again after an
    /// admin reactivated it.
    /// </summary>
    Task<string> RenderJobReactivatedEmailAsync(
        string recipientName,
        string jobTitle,
        string jobUrl,
        CancellationToken cancellationToken = default
    );
}