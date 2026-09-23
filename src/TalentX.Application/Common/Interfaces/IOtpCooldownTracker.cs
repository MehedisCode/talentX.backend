namespace TalentX.Application.Common.Interfaces;

public interface IOtpCooldownTracker
{
    Task<bool> IsOnCooldownAsync(string key, CancellationToken cancellationToken = default);

    Task<int> GetRemainingCooldownSecondsAsync(string key, CancellationToken cancellationToken = default);

    Task SetCooldownAsync(string key, TimeSpan duration, CancellationToken cancellationToken = default);

    Task ClearCooldownAsync(string key, CancellationToken cancellationToken = default);
}
