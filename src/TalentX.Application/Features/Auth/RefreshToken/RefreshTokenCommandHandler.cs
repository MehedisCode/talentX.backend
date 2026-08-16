using MediatR;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Identity.Models;

namespace TalentX.Application.Features.Auth.RefreshToken;

public class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, IdentityResponse<TokenResponse>>
{
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<IdentityResponse<TokenResponse>> Handle(
        RefreshTokenCommand request, CancellationToken ct)
    {
        return await _tokenService.ValidateAndRotateAsync(request.RefreshToken);
    }
}