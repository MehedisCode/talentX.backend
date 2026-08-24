using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.Auth;

public sealed class GoogleAuthService : IGoogleAuthService
{
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly string _clientId;

    public GoogleAuthService(ILogger<GoogleAuthService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _clientId = configuration["GOOGLE_CLIENT_ID"]
            ?? configuration["Google:ClientId"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")?.Trim()
            ?? string.Empty;
    }

    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings();

            if (!string.IsNullOrWhiteSpace(_clientId))
            {
                settings.Audience = [_clientId];
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo(
                GoogleId: payload.Subject,
                Email: payload.Email,
                Name: payload.Name ?? payload.Email
            );
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Google ID token validation failed: {Reason}", ex.Message);
            throw new UnauthorizedException("Invalid or expired Google ID token.");
        }
        catch (Exception ex) when (ex is not UnauthorizedException)
        {
            _logger.LogError(ex, "Unexpected error during Google ID token verification: {Message}", ex.Message);
            throw new UnauthorizedException("Unable to verify Google credentials.");
        }
    }
}
