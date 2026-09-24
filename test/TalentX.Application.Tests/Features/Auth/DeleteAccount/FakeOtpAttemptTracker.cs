using TalentX.Application.Common.Constants;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Tests.Features.Auth.DeleteAccount;

internal sealed class FakeOtpAttemptTracker : IOtpAttemptTracker
{
    private readonly Dictionary<string, int> _failures = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsBlockedAsync(string otpKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(_failures.TryGetValue(otpKey, out var count) && count >= OtpAttemptPolicy.MaxFailedAttempts);

    public Task RecordFailureAsync(string otpKey, CancellationToken cancellationToken = default)
    {
        _failures[otpKey] = _failures.GetValueOrDefault(otpKey) + 1;
        return Task.CompletedTask;
    }

    public Task ResetAsync(string otpKey, CancellationToken cancellationToken = default)
    {
        _failures.Remove(otpKey);
        return Task.CompletedTask;
    }
}
