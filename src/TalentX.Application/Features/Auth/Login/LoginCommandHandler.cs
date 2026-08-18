using MediatR;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Identity.Models;

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
        var passwordCheck = await _identityService.ValidatePasswordAsync(
            request.Email, 
            request.Password);

        if (!passwordCheck.Success)
        {
            return IdentityResponse<LoginResult>.Fail(passwordCheck.Errors);
        }

        var tokens = await _tokenService.CreateAsync(passwordCheck.Data!.UserId);

        if (!tokens.Success)
        {
            return IdentityResponse<LoginResult>.Fail(tokens.Errors);
        }

        return IdentityResponse<LoginResult>.Ok(
            new LoginResult
            {
                UserId = passwordCheck.Data.UserId,
                Email = passwordCheck.Data.Email,
                Tokens = tokens.Data!
            });
    }
}