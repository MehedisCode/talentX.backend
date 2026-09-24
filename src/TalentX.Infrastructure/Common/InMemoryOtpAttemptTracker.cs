using System.Collections.Concurrent;
using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Common;

public sealed class InMemoryOtpAttemptTracker : IOtpAttemptTracker
{
    private sealed record Attempt(int Count, DateTime LastFailureUtc);

    private readonly ConcurrentDictionary<string, Attempt> _attempts = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsBlockedAsync(string otpKey, CancellationToken cancellationToken = default)
    {
        if (!_attempts.TryGetValue(otpKey, out var attempt))
        {
            return Task.FromResult(false);
        }

        if (IsExpired(attempt))
        {
            _attempts.TryRemove(otpKey, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(attempt.Count >= OtpAttemptPolicy.MaxFailedAttempts);
    }

    public Task RecordFailureAsync(string otpKey, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        _attempts.AddOrUpdate(
            otpKey,
            _ => new Attempt(1, now),
            (_, existing) => IsExpired(existing)
                ? new Attempt(1, now)
                : existing with { Count = existing.Count + 1, LastFailureUtc = now });

        return Task.CompletedTask;
    }

    public Task ResetAsync(string otpKey, CancellationToken cancellationToken = default)
    {
        _attempts.TryRemove(otpKey, out _);
        return Task.CompletedTask;
    }

    private static bool IsExpired(Attempt attempt) =>
        DateTime.UtcNow - attempt.LastFailureUtc > OtpAttemptPolicy.AttemptWindow;
}
