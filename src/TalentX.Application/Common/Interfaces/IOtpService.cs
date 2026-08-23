namespace TalentX.Application.Common.Interfaces;

public interface IOtpService
{
    Task<string> GenerateOtpAsync(
        string key,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> VerifyOtpAsync(
        string key,
        string otpCode,
        CancellationToken cancellationToken = default
    );

    Task MarkEmailVerifiedAsync(
        string email,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> IsEmailVerifiedAsync(
        string email,
        CancellationToken cancellationToken = default
    );

    Task ConsumeEmailVerificationAsync(
        string email,
        CancellationToken cancellationToken = default
    );
}
