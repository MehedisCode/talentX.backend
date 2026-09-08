namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Service contract for managing Identity users provisioned via external OAuth providers.
/// </summary>
public interface IExternalIdentityService
{
    /// <summary>
    /// Finds an existing Identity user by email, or creates a passwordless user for external OAuth sign-in.
    /// Assigns the specified role if the user is newly created.
    /// </summary>
    /// <param name="email">User email address.</param>
    /// <param name="role">Role name to assign if newly created.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple indicating if the user was newly created and their unique Identity user identifier.</returns>
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
