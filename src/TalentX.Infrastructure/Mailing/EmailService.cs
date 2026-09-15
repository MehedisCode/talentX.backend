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

    public Task<string> RenderInterviewInvitationEmailAsync(
        string recipientName,
        string jobTitle,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        LogDispatch(EmailConstants.Kinds.InterviewInvitation, recipientName, jobTitle, companyName);

        return _razor.CompileRenderAsync(
            EmailConstants.Templates.InterviewInvitation,
            new InterviewInvitationEmailModel(recipientName, jobTitle, companyName));
    }

    public Task<string> RenderInterviewConfirmationEmailAsync(
        string recipientName,
        string jobTitle,
        string companyName,
        string interviewType,
        string locationOrLink,
        string scheduledTimeOrDeadline,
        string? instructions,
        CancellationToken cancellationToken = default)
    {
        LogDispatch(EmailConstants.Kinds.InterviewConfirmation, recipientName, jobTitle, companyName);

        return _razor.CompileRenderAsync(
            EmailConstants.Templates.InterviewConfirmation,
            new InterviewConfirmationEmailModel(
                recipientName, jobTitle, companyName, interviewType, locationOrLink, scheduledTimeOrDeadline, instructions));
    }

    public async Task DispatchDirectAsync(
        string toEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser))
        {
            _logger.LogWarning("SMTP not configured, skipping direct dispatch for {ToEmail}.", toEmail);
            throw new InvalidOperationException($"SMTP settings missing (host='{_smtpHost}', user='{_smtpUser}').");
        }

        await SendSmtpAsync(toEmail, recipientName, subject, htmlBody);
    }

    public async Task SendJobInvitationEmailAsync(
        string toEmail,
        string recipientName,
        string jobTitle,
        string companyName,
        string jobUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] JobInvitation dispatch to {RecipientName} <{ToEmail}> for role {JobTitle} at {CompanyName}: {JobUrl}",
            recipientName, toEmail, jobTitle, companyName, jobUrl);

        var subject = $"You've been invited to apply for {jobTitle} at {companyName}!";
        var html = await _razor.CompileRenderAsync(
            EmailConstants.Templates.JobInvitation,
            new JobInvitationEmailModel(recipientName, jobTitle, companyName, jobUrl));

        DispatchAfterLogging(toEmail, recipientName, subject, html);
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] {Kind} dispatch to {RecipientName} <{ToEmail}>: {Url}",
            EmailConstants.Kinds.PasswordReset, recipientName, toEmail, resetUrl);

        var html = await _razor.CompileRenderAsync(
            EmailConstants.Templates.PasswordReset,
            new PasswordResetEmailModel(recipientName, resetUrl));

        DispatchAfterLogging(toEmail, recipientName, EmailConstants.Subjects.PasswordReset, html);
    }

    public async Task SendEmployerApprovedEmailAsync(
        string toEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] {Kind} dispatch to {RecipientName} <{ToEmail}>",
            EmailConstants.Kinds.EmployerApproved, recipientName, toEmail);

        var html = await _razor.CompileRenderAsync(
            EmailConstants.Templates.EmployerApproved,
            new EmployerApprovedEmailModel(recipientName));

        DispatchAfterLogging(toEmail, recipientName, EmailConstants.Subjects.EmployerApproved, html);
    }

    public async Task SendEmployerRejectedEmailAsync(
        string toEmail,
        string recipientName,
        string rejectionReason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[EMAIL] {Kind} dispatch to {RecipientName} <{ToEmail}>",
            EmailConstants.Kinds.EmployerRejected, recipientName, toEmail);

        var html = await _razor.CompileRenderAsync(
            EmailConstants.Templates.EmployerRejected,
            new EmployerRejectedEmailModel(recipientName, rejectionReason));

        DispatchAfterLogging(toEmail, recipientName, EmailConstants.Subjects.EmployerRejected, html);
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
            await SendSmtpAsync(toEmail, recipientName, subject, htmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Failed to dispatch to {ToEmail}.", toEmail);
        }
    }

    private async Task SendSmtpAsync(string toEmail, string recipientName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_senderName, _senderEmail));
        message.To.Add(new MailboxAddress(recipientName, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient
        {
            Timeout = 15000 // 15s to allow DNS + TCP + TLS handshake over remote latency
        };

        client.ServerCertificateValidationCallback = (_, certificate, chain, errors) =>
        {
            if (errors == System.Net.Security.SslPolicyErrors.None)
                return true;

            return chain?.ChainStatus.All(s =>
                s.Status is System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError
                         or System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.RevocationStatusUnknown
                         or System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.OfflineRevocation) ?? false;
        };

        var socketOptions = _smtpPort switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 or 2587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto
        };

        await client.ConnectAsync(_smtpHost, _smtpPort, socketOptions);
        await client.AuthenticateAsync(_smtpUser, _smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);

        _logger.LogInformation("[EMAIL] Dispatched successfully to {ToEmail} via {Host}:{Port}.", toEmail, _smtpHost, _smtpPort);
    }

    private static string Env(string key) =>
        Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;
}
