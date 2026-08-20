using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Common;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IOtpService"/>.
/// Generates 6-digit cryptographic OTPs, outputs them prominently to the application log
/// across all environments for developer/QA verification, and manages time-based expiry.
/// </summary>
public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAtUtc)> _otpStore = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<OtpService> _logger;

    public OtpService(ILogger<OtpService> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateOtpAsync(
        string key,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var effectiveExpiry = expiry ?? DefaultExpiry;
        var expiresAt = DateTime.UtcNow.Add(effectiveExpiry);

        _otpStore[key] = (code, expiresAt);

        _logger.LogInformation(
            "\n======================================================\n🔑 [TALENTX OTP DISPATCH]\n👉 Target Email : {Email}\n👉 OTP CODE     : {OtpCode}\n⏳ Expires In   : {Minutes} minutes\n======================================================\n",
            key,
            code,
            effectiveExpiry.TotalMinutes);

        return Task.FromResult(code);
    }

    public Task<bool> VerifyOtpAsync(
        string key,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        if (!_otpStore.TryGetValue(key, out var record))
        {
            _logger.LogWarning("⚠️ [TALENTX OTP] Verification failed for '{Email}': No active OTP record found.", key);
            return Task.FromResult(false);
        }

        if (DateTime.UtcNow > record.ExpiresAtUtc)
        {
            _logger.LogWarning("⚠️ [TALENTX OTP] Verification failed for '{Email}': OTP code has expired.", key);
            _otpStore.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        if (!string.Equals(record.Code, otpCode, StringComparison.Ordinal))
        {
            _logger.LogWarning("⚠️ [TALENTX OTP] Verification failed for '{Email}': Code mismatch.", key);
            return Task.FromResult(false);
        }

        _logger.LogInformation("✅ [TALENTX OTP] Verification successful for '{Email}'.", key);

        // Invalidate OTP immediately after successful verification
        _otpStore.TryRemove(key, out _);
        return Task.FromResult(true);
    }
}
