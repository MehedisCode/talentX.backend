namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Service contract for ASP.NET Core Identity authentication and user management operations.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Creates a new user in the Identity store and assigns the specified role.
    /// </summary>
    /// <param name="email">User email address.</param>
    /// <param name="password">Plain text password.</param>
    /// <param name="role">Role name to assign (e.g. "Employer").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result tuple indicating success, user string identifier, and any errors.</returns>
    Task<(bool Succeeded, string UserId, IEnumerable<string> Errors)> CreateUserAsync(
        string email,
        string password,
        string role,
        CancellationToken cancellationToken = default
    );
}
