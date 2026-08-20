namespace TalentX.Application.DTOs.Employers;

/// <summary>
/// Response data transfer object representing an authenticated employer along with security tokens.
/// </summary>
/// <param name="Employer">The employer profile information.</param>
/// <param name="AccessToken">JWT access token for authenticating subsequent requests.</param>
/// <param name="RefreshToken">Refresh token for renewing the access token session.</param>
public sealed record AuthEmployerDto(
    EmployerDto Employer,
    string AccessToken,
    string RefreshToken
);
