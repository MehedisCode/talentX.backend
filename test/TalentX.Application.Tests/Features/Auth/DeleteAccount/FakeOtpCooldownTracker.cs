using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Tests.Features.Auth.DeleteAccount;

internal sealed class FakeOtpCooldownTracker : IOtpCooldownTracker
{
    private readonly Dictionary<string, DateTime> _cooldowns = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _now = DateTime.UtcNow;

    public void AdvanceTime(TimeSpan delta) => _now = _now.Add(delta);

    public Task<bool> IsOnCooldownAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cooldowns.TryGetValue(key, out var expiry) && _now < expiry);

    public Task<int> GetRemainingCooldownSecondsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cooldowns.TryGetValue(key, out var expiry) && _now < expiry)
        {
            return Task.FromResult((int)Math.Ceiling((expiry - _now).TotalSeconds));
        }

        return Task.FromResult(0);
    }

    public Task SetCooldownAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        _cooldowns[key] = _now.Add(duration);
        return Task.CompletedTask;
    }

    public Task ClearCooldownAsync(string key, CancellationToken cancellationToken = default)
    {
        _cooldowns.Remove(key);
        return Task.CompletedTask;
    }
}
