using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using RazorLight;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Mailing.Models;

namespace TalentX.Infrastructure.Mailing;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IRazorLightEngine _razor;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
        _razor = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(EmailService).Assembly)
            .SetOperatingAssembly(typeof(EmailService).Assembly)
            .Build();

        _smtpHost    = Env("SMTP_HOST");
        _smtpPort    = int.TryParse(Env("SMTP_PORT"), out var p) ? p : 587;
        _smtpUser    = Env("SMTP_USER");
        _smtpPass    = Env("SMTP_PASS");
        _senderEmail = Env("SENDER_EMAIL");
        _senderName  = Env("SENDER_NAME") is { Length: > 0 } n ? n : EmailConstants.Defaults.SenderName;
    }

    public async Task SendOtpEmailAsync(
        string toEmail,
        string recipientName,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        LogDispatch(EmailConstants.Kinds.Otp, recipientName, toEmail, otpCode);

        var html = await _razor.CompileRenderAsync(
            EmailConstants.Templates.Otp,
            new OtpEmailModel(recipientName, otpCode));

        DispatchAfterLogging(toEmail, recipientName, EmailConstants.Subjects.Otp, html);
    }

    public async Task SendCredentialsEmailAsync(
        string toEmail,
        string recipientName,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        LogDispatch(EmailConstants.Kinds.Credentials, recipientName, toEmail, temporaryPassword);

        var html = await _razor.CompileRenderAsync(
            EmailConstants.Templates.Credentials,
            new CredentialsEmailModel(recipientName, toEmail, temporaryPassword));

        DispatchAfterLogging(toEmail, recipientName, EmailConstants.Subjects.Credentials, html);
    }

    public async Task SendVerificationLinkEmailAsync(
        string toEmail,
        string recipientName,
        string verificationUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] {Kind} dispatch to {RecipientName} <{ToEmail}>: {Url}",
            EmailConstants.Kinds.VerificationLink, recipientName, toEmail, verificationUrl);

        var html = await _razor.CompileRenderAsync(
            EmailConstants.Templates.VerificationLink,
            new VerificationLinkEmailModel(recipientName, verificationUrl));

        DispatchAfterLogging(toEmail, recipientName, EmailConstants.Subjects.VerificationLink, html);
    }

    public async Task SendJobInvitationEmailAsync(
        string toEmail,
        string recipientName,
        string jobTitle,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] JobInvitation dispatch to {RecipientName} <{ToEmail}> for role {JobTitle} at {CompanyName}",
            recipientName, toEmail, jobTitle, companyName);

        var subject = $"You've been invited to apply for {jobTitle} at {companyName}!";
        var html = $"<p>Hi {recipientName},</p><p>Great news! <strong>{companyName}</strong> has invited you to view and apply for the <strong>{jobTitle}</strong> position on TalentX.</p>";

        DispatchAfterLogging(toEmail, recipientName, subject, html);
    }

    private void LogDispatch(string kind, string recipientName, string toEmail, string? secret)
    {
        _logger.LogInformation(
            "[EMAIL] {Kind} dispatch to {RecipientName} <{ToEmail}>: {Secret}",
            kind, recipientName, toEmail, secret);
    }

    private void DispatchAfterLogging(string toEmail, string recipientName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser))
        {
            _logger.LogWarning("SMTP not configured, email logged only (host={Host}, user={User})", _smtpHost, _smtpUser);
            return;
        }

        _ = Task.Run(() => DispatchAsync(toEmail, recipientName, subject, htmlBody), CancellationToken.None);
    }

    private async Task DispatchAsync(string toEmail, string recipientName, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress(recipientName, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            client.ServerCertificateValidationCallback = (_, certificate, chain, errors) =>
            {
                if (errors == System.Net.Security.SslPolicyErrors.None)
                    return true;

                return chain?.ChainStatus.All(s =>
                    s.Status is System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError
                             or System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.RevocationStatusUnknown
                             or System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.OfflineRevocation) ?? false;
            };

            var socketOptions = _smtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_smtpHost, _smtpPort, socketOptions);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            _logger.LogInformation("[EMAIL] Dispatched successfully to {ToEmail} via {Host}:{Port}.", toEmail, _smtpHost, _smtpPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Failed to dispatch to {ToEmail}.", toEmail);
        }
    }

    private static string Env(string key) =>
        Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;
}
