using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Mailing;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _senderEmail;
    private readonly string _senderName;

    public EmailService(ILogger<EmailService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;

        _smtpHost    = Env("SMTP_HOST");
        _smtpPort    = int.TryParse(Env("SMTP_PORT"), out var p) ? p : 587;
        _smtpUser    = Env("SMTP_USER");
        _smtpPass    = Env("SMTP_PASS");
        _senderEmail = Env("SENDER_EMAIL");
        _senderName  = Env("SENDER_NAME") is { Length: > 0 } n ? n : "TalentX Support";
    }

    public Task SendOtpEmailAsync(
        string toEmail,
        string recipientName,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        LogDispatch("OTP", recipientName, toEmail, otpCode);

        DispatchAfterLogging(
            toEmail,
            recipientName,
            "Your TalentX Verification Code",
            BuildOtpHtml(recipientName, otpCode));

        return Task.CompletedTask;
    }

    public Task SendCredentialsEmailAsync(
        string toEmail,
        string recipientName,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        LogDispatch("CREDENTIALS", recipientName, toEmail, temporaryPassword);

        DispatchAfterLogging(
            toEmail,
            recipientName,
            "Your TalentX Login Credentials",
            BuildCredentialsHtml(recipientName, toEmail, temporaryPassword));

        return Task.CompletedTask;
    }

    private void LogDispatch(string kind, string recipientName, string toEmail, string? secret)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "[EMAIL] {Kind} dispatch to {RecipientName} <{ToEmail}>: {Secret}",
                kind, recipientName, toEmail, secret);
        }
        else
        {
            _logger.LogInformation(
                "[EMAIL] {Kind} dispatch to {RecipientName} <{ToEmail}>",
                kind, recipientName, toEmail);
        }
    }

    private void DispatchAfterLogging(string toEmail, string recipientName, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser))
        {
            _logger.LogWarning("SMTP not configured, email logged only (host={Host}, user={User})", _smtpHost, _smtpUser);
            return;
        }

        // Fire-and-forget so the HTTP response returns immediately
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

            // macOS/.NET cannot always complete CRL/OCSP revocation checks.
            // Accept the certificate if the only chain errors are revocation-related.
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

    private static string BuildOtpHtml(string name, string otp) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Verify Your TalentX Account</title></head>
        <body style="font-family:Arial,sans-serif;background:#f4f6f8;margin:0;padding:20px;">
          <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:8px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,.05);">
            <h2 style="color:#111827;margin-top:0;">Verify Your Email Address</h2>
            <p style="color:#4b5563;font-size:15px;line-height:1.5;">
              Hello {System.Net.WebUtility.HtmlEncode(name)},<br><br>
              Use the code below to complete your TalentX registration. It expires in <strong>10 minutes</strong>.
            </p>
            <div style="text-align:center;margin:28px 0;">
              <span style="display:inline-block;font-size:32px;font-weight:700;letter-spacing:6px;color:#2563eb;background:#eff6ff;padding:12px 24px;border-radius:8px;border:1px dashed #93c5fd;">
                {otp}
              </span>
            </div>
            <p style="color:#6b7280;font-size:13px;">If you did not request this, please ignore this email.</p>
            <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;">
            <p style="color:#9ca3af;font-size:12px;text-align:center;margin:0;">&copy; {DateTime.UtcNow.Year} TalentX Inc.</p>
          </div>
        </body>
        </html>
        """;

    private static string BuildCredentialsHtml(string name, string email, string password) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Your TalentX Login Credentials</title></head>
        <body style="font-family:Arial,sans-serif;background:#f4f6f8;margin:0;padding:20px;">
          <div style="max-width:520px;margin:0 auto;background:#fff;border-radius:8px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,.05);">
            <h2 style="color:#111827;margin-top:0;">Your TalentX Account</h2>
            <p style="color:#4b5563;font-size:15px;line-height:1.5;">
              Hello {System.Net.WebUtility.HtmlEncode(name)},<br><br>
              Thanks for applying on TalentX. An account was created for you automatically.
              Use the credentials below to sign in and track your applications.
            </p>
            <div style="background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;padding:20px;margin:24px 0;">
              <p style="margin:0 0 8px;color:#6b7280;font-size:13px;">Email</p>
              <p style="margin:0 0 16px;font-size:15px;font-weight:600;color:#111827;">{System.Net.WebUtility.HtmlEncode(email)}</p>
              <p style="margin:0 0 8px;color:#6b7280;font-size:13px;">Temporary password</p>
              <p style="margin:0;font-size:15px;font-weight:700;letter-spacing:1px;color:#111827;">{System.Net.WebUtility.HtmlEncode(password)}</p>
            </div>
            <p style="color:#6b7280;font-size:13px;">For security, please change this password after signing in.</p>
            <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;">
            <p style="color:#9ca3af;font-size:12px;text-align:center;margin:0;">&copy; {DateTime.UtcNow.Year} TalentX Inc.</p>
          </div>
        </body>
        </html>
        """;
}