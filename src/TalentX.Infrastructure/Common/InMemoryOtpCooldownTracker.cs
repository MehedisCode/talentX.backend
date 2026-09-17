using System.Collections.Concurrent;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Common;

public sealed class InMemoryOtpCooldownTracker : IOtpCooldownTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _cooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeProvider _timeProvider;

    public InMemoryOtpCooldownTracker(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<bool> IsOnCooldownAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_cooldowns.TryGetValue(key, out var expiryUtc))
        {
            return Task.FromResult(false);
        }

        if (_timeProvider.GetUtcNow().UtcDateTime >= expiryUtc)
        {
            _cooldowns.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task<int> GetRemainingCooldownSecondsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_cooldowns.TryGetValue(key, out var expiryUtc))
        {
            return Task.FromResult(0);
        }

        var diff = expiryUtc - _timeProvider.GetUtcNow().UtcDateTime;
        if (diff <= TimeSpan.Zero)
        {
            _cooldowns.TryRemove(key, out _);
            return Task.FromResult(0);
        }

        return Task.FromResult((int)Math.Ceiling(diff.TotalSeconds));
    }

    public Task SetCooldownAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var expiryUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(duration);
        _cooldowns[key] = expiryUtc;
        return Task.CompletedTask;
    }

    public Task ClearCooldownAsync(string key, CancellationToken cancellationToken = default)
    {
        _cooldowns.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
