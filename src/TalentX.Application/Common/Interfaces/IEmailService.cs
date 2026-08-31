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
}