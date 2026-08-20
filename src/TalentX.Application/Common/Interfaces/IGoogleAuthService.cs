namespace TalentX.Application.Common.Interfaces;

/// <summary>
/// Service contract for verifying Google OAuth ID tokens and extracting user profile claims.
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>
    /// Validates the provided Google ID token against Google's public key infrastructure.
    /// </summary>
    /// <param name="idToken">The JWT ID token from Google Sign-In.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted and verified Google user profile.</returns>
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Verified profile information extracted from a valid Google ID token.
/// </summary>
/// <param name="GoogleId">Google's unique subject identifier (sub claim).</param>
/// <param name="Email">Verified Google account email address.</param>
/// <param name="Name">Display name from the Google account.</param>
public sealed record GoogleUserInfo(string GoogleId, string Email, string Name);
