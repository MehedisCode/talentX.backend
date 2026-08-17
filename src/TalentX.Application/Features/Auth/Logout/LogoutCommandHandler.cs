using MediatR;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Identity.Models;

namespace TalentX.Application.Features.Auth.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, IdentityResponse>
{
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<IdentityResponse> Handle(LogoutCommand request, CancellationToken ct)
    {
        return await _tokenService.RevokeAsync(request.RefreshToken);
    }
}