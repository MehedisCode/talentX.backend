using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IEmailService"/> with structured logging for development and delivery.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendOtpEmailAsync(
        string toEmail,
        string recipientName,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "===============================================================\n" +
            " [EMAIL SERVICE - OTP VERIFICATION]\n" +
            " To: {RecipientName} <{ToEmail}>\n" +
            " OTP Code: {OtpCode}\n" +
            " Please enter this 6-digit code to verify your account.\n" +
            "===============================================================",
            recipientName,
            toEmail,
            otpCode
        );

        return Task.CompletedTask;
    }
}
