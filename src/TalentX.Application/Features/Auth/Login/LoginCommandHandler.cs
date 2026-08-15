using MediatR;
using TalentX.Application.Common.Identity.Interfaces;
using TalentX.Application.Common.Identity.Models;
using TalentX.Application.Features.Auth.LogIn;

namespace TalentX.Application.Features.Auth.Login;

public class LoginCommandHandler 
    : IRequestHandler<LoginCommand, IdentityResponse>
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

    public async Task<IdentityResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var validation = await _identityService.ValidatePasswordAsync(request.Email, request.Password);
        if (!validation.Success)
        {
            return validation;
        }

        // adjust the cast to whatever shape ValidatePasswordAsync actually returns
        // 'dynamic' is C#'s escape hatch from the compiler's normal type checking 
        // it tells the compiler "don't verify this at compile time, 
        // figure out what to do with it at runtime instead.
        dynamic data = validation.Data!;
        string userId = data.UserId;

        var response = await _tokenService.CreateAsync(userId);
        
        return response;
    }
}