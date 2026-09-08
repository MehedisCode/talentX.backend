using MediatR;
using Microsoft.Extensions.Configuration;
using TalentX.Application.Identity.Interfaces;

namespace TalentX.Application.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IIdentityService _identityService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IConfiguration configuration)
    {
        _identityService = identityService;
        _configuration = configuration;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var frontendUrl = ResolveFrontendUrl();
        return await _identityService.InitiatePasswordResetAsync(request.Email, frontendUrl, cancellationToken);
    }

    private string ResolveFrontendUrl()
    {
        var rawCors = _configuration["CORS_ALLOWED_ORIGINS"]
                   ?? _configuration["Cors:AllowedOrigins"]
                   ?? _configuration["Cors:FrontendUrl"];

        if (!string.IsNullOrWhiteSpace(rawCors))
        {
            var origins = rawCors.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var remoteOrigin = origins.FirstOrDefault(origin => !IsLocalhost(origin));
            if (!string.IsNullOrWhiteSpace(remoteOrigin))
            {
                return remoteOrigin.TrimEnd('/');
            }
        }

        return "http://localhost:3000";
    }

    private static bool IsLocalhost(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.IsLoopback ||
                   uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.Equals("127.0.0.1");
        }

        return url.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("127.0.0.1");
    }
}
