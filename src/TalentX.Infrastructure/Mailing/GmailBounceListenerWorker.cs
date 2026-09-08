using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Mailing;

public sealed class GmailBounceListenerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailBounceListenerWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);

    public GmailBounceListenerWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<GmailBounceListenerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[IMAP] GmailBounceListenerWorker started. Checking inbox every {Interval}s.", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessGmailBouncesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IMAP] Unexpected error checking Gmail inbox for bounce notifications.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("[IMAP] GmailBounceListenerWorker stopped.");
    }

    private async Task ProcessGmailBouncesAsync(CancellationToken stoppingToken)
    {
        var smtpHost = GetConfig("SMTP_HOST", "EmailSettings:Host");
        var smtpUser = GetConfig("SMTP_USER", "EmailSettings:UserName");
        var smtpPass = GetConfig("SMTP_PASS", "EmailSettings:Password");

        if (string.IsNullOrWhiteSpace(smtpHost) && smtpUser.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            smtpHost = "smtp.gmail.com";
        }

        if (string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass))
        {
            _logger.LogWarning("[IMAP] GmailBounceListenerWorker: Skipped IMAP check because SMTP_USER or SMTP_PASS is missing.");
            return;
        }

        if (!smtpHost.Contains("gmail", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[IMAP] GmailBounceListenerWorker: Host '{Host}' is non-Gmail. IMAP bounce listener dormant.", smtpHost);
            return;
        }

        _logger.LogInformation("[IMAP] Connecting to imap.gmail.com:993 for account '{User}'...", smtpUser);

        using var client = new ImapClient();
        client.Timeout = 15000;

        try
        {
            await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, stoppingToken);
            await client.AuthenticateAsync(smtpUser, smtpPass, stoppingToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, stoppingToken);

            // Unread bounce notifications from Google's mailer daemon or postmaster.
            var query = SearchQuery.NotSeen.And(
                SearchQuery.FromContains("mailer-daemon")
                .Or(SearchQuery.FromContains("postmaster"))
                .Or(SearchQuery.SubjectContains("Delivery Status Notification")));

            var uids = await inbox.SearchAsync(query, stoppingToken);
            if (uids.Count == 0)
            {
                await client.DisconnectAsync(true, stoppingToken);
                return;
            }

            _logger.LogInformation("[IMAP] Found {Count} unread bounce notifications in Gmail Inbox.", uids.Count);

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, stoppingToken);
                var text = message.TextBody ?? message.HtmlBody ?? string.Empty;

                var matches = Regex.Matches(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
                foreach (Match match in matches)
                {
                    var extractedEmail = match.Value;

                    if (extractedEmail.Equals(smtpUser, StringComparison.OrdinalIgnoreCase) ||
                        extractedEmail.StartsWith("mailer-daemon@", StringComparison.OrdinalIgnoreCase) ||
                        extractedEmail.StartsWith("postmaster@", StringComparison.OrdinalIgnoreCase) ||
                        extractedEmail.Equals("mailer-daemon@googlemail.com", StringComparison.OrdinalIgnoreCase) ||
                        extractedEmail.Equals("mailer-daemon@google.com", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    _logger.LogInformation("[IMAP] Checking DB for bounced email candidate '{Email}'...", extractedEmail);

                    var log = await unitOfWork.EmailDispatchLogs.GetLatestByEmailAsync(extractedEmail, stoppingToken);
                    if (log != null)
                    {
                        log.MarkFailed("Gmail Bounce: Delivery Status Notification (Address not found / Mailbox bounced)", TimeSpan.Zero);
                        await unitOfWork.SaveChangesAsync(stoppingToken);

                        _logger.LogWarning("[IMAP] Matched bounce! Updated LogId {LogId} for '{Email}' to Failed.", log.Id, extractedEmail);
                        break;
                    }
                }

                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, stoppingToken);
            }

            await client.DisconnectAsync(true, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[IMAP] Failed to process Gmail bounce notifications.");
        }
    }

    private string GetConfig(string envKey, string configKey)
    {
        var envVal = Environment.GetEnvironmentVariable(envKey)?.Trim();
        if (!string.IsNullOrWhiteSpace(envVal))
        {
            return envVal;
        }

        return _configuration[envKey]?.Trim()
            ?? _configuration[configKey]?.Trim()
            ?? string.Empty;
    }
}
