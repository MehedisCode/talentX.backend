namespace TalentX.Application.Common.Interfaces;

public interface IOtpAttemptTracker
{
    Task<bool> IsBlockedAsync(string otpKey, CancellationToken cancellationToken = default);

    Task RecordFailureAsync(string otpKey, CancellationToken cancellationToken = default);

    Task ResetAsync(string otpKey, CancellationToken cancellationToken = default);
}
