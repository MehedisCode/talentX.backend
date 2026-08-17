using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IOtpService"/>.
/// Completely standalone and decoupled from ASP.NET Core Identity.
/// </summary>
public sealed class OtpService : IOtpService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAtUtc)> _otpStore = new(StringComparer.OrdinalIgnoreCase);
    private readonly IHostEnvironment _environment;

    public OtpService(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<string> GenerateOtpAsync(
        string key,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAt = DateTime.UtcNow.Add(expiry ?? DefaultExpiry);

        _otpStore[key] = (code, expiresAt);

        return Task.FromResult(code);
    }

    public Task<bool> VerifyOtpAsync(
        string key,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        // In Development environment, bypass with magic OTP "000000"
        if (_environment.IsDevelopment() && otpCode == "000000")
        {
            _otpStore.TryRemove(key, out _);
            return Task.FromResult(true);
        }

        if (!_otpStore.TryGetValue(key, out var record))
        {
            return Task.FromResult(false);
        }

        if (DateTime.UtcNow > record.ExpiresAtUtc)
        {
            _otpStore.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        if (!string.Equals(record.Code, otpCode, StringComparison.Ordinal))
        {
            return Task.FromResult(false);
        }

        // Invalidate OTP after successful verification
        _otpStore.TryRemove(key, out _);
        return Task.FromResult(true);
    }
}
