using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Common;

public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DefaultVerifiedEmailExpiry = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAtUtc)> _otpStore = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _verifiedEmailStore = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<OtpService> _logger;
    private readonly IHostEnvironment _environment;

    public OtpService(ILogger<OtpService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
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

        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "OTP generated for {Email}: {OtpCode} (expires in {ExpiryMinutes} minutes)",
                key,
                code,
                effectiveExpiry.TotalMinutes);
        }
        else
        {
            _logger.LogInformation(
                "OTP generated for {Email} (expires in {ExpiryMinutes} minutes)",
                key,
                effectiveExpiry.TotalMinutes);
        }

        return Task.FromResult(code);
    }

    public Task<bool> VerifyOtpAsync(
        string key,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        if (!_otpStore.TryGetValue(key, out var record))
        {
            _logger.LogWarning("OTP verification failed for {Email}: no active OTP record found", key);
            return Task.FromResult(false);
        }

        if (DateTime.UtcNow > record.ExpiresAtUtc)
        {
            _logger.LogWarning("OTP verification failed for {Email}: code expired", key);
            _otpStore.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        if (!string.Equals(record.Code, otpCode, StringComparison.Ordinal))
        {
            _logger.LogWarning("OTP verification failed for {Email}: code mismatch", key);
            return Task.FromResult(false);
        }

        _logger.LogInformation("OTP verified for {Email}", key);

        _otpStore.TryRemove(key, out _);
        return Task.FromResult(true);
    }

    public Task MarkEmailVerifiedAsync(
        string email,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveExpiry = expiry ?? DefaultVerifiedEmailExpiry;
        var expiresAtUtc = DateTime.UtcNow.Add(effectiveExpiry);

        _verifiedEmailStore[email] = expiresAtUtc;
        _logger.LogInformation("Email {Email} marked as verified in cache (expires in {ExpiryMinutes} minutes)", email, effectiveExpiry.TotalMinutes);

        return Task.CompletedTask;
    }

    public Task<bool> IsEmailVerifiedAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (!_verifiedEmailStore.TryGetValue(email, out var expiresAtUtc))
        {
            return Task.FromResult(false);
        }

        if (DateTime.UtcNow > expiresAtUtc)
        {
            _verifiedEmailStore.TryRemove(email, out _);
            _logger.LogWarning("Email verification check failed for {Email}: verification session expired", email);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task ConsumeEmailVerificationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        _verifiedEmailStore.TryRemove(email, out _);
        _logger.LogInformation("Email verification session consumed for {Email}", email);
        return Task.CompletedTask;
    }
}
