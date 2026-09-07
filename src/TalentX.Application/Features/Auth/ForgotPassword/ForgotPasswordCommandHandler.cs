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
        var isDevelopment = string.Equals(
            _configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            "Development",
            StringComparison.OrdinalIgnoreCase);

        var defaultFallbackUrl = isDevelopment ? "http://localhost:3000" : "https://talentx-stg-app.ait-us.co";

        var frontendUrl = _configuration["Frontend:Url"]
                       ?? _configuration["App:FrontendUrl"]
                       ?? _configuration["Cors:FrontendUrl"]
                       ?? defaultFallbackUrl;

        return await _identityService.InitiatePasswordResetAsync(request.Email, frontendUrl, cancellationToken);
    }
}
