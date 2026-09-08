namespace TalentX.Application.Common.Interfaces;

public interface IExternalIdentityService
{
    Task<(bool IsNewUser, string UserId)> FindOrCreateExternalUserAsync(
        string email,
        string role,
        CancellationToken cancellationToken = default
    );

    Task<(bool IsNewUser, string UserId)> FindOrCreateExternalUserAsync(
        string email,
        string role,
        string? providerKey,
        CancellationToken cancellationToken = default
    );
}
