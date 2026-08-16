using MediatR;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Identity.Models;
using TalentX.Application.Features.Auth.LogIn;

namespace TalentX.Application.Features.Auth.Login;

public class LoginCommandHandler 
    : IRequestHandler<LoginCommand, IdentityResponse<LoginResult>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IIdentityService identityService, 
        ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<IdentityResponse<LoginResult>> Handle(LoginCommand request, CancellationToken ct)
    {
        var validation = await _identityService.ValidatePasswordAsync(request.Email, request.Password);
        if (!validation.Success)
        {
            return IdentityResponse<LoginResult>.Fail(validation.Errors);
        }

        var userId = validation.Data!.UserId;

        var tokenResult = await _tokenService.CreateAsync(userId);
        if (!tokenResult.Success)
        {
            return IdentityResponse<LoginResult>.Fail(tokenResult.Errors);
        }

        return IdentityResponse<LoginResult>.Ok(new LoginResult
        {
            UserId = userId,
            Email = validation.Data.Email,
            Tokens = tokenResult.Data!
        });
    }
}