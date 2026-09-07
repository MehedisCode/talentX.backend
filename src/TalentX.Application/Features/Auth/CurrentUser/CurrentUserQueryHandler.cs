using MediatR;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Auth.CurrentUser;

public class CurrentUserQueryHandler
    : IRequestHandler<CurrentUserQuery, IdentityResponse<CurrentUserResult>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public CurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<IdentityResponse<CurrentUserResult>> Handle(
        CurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated
            || _currentUserService.UserId is null
            || _currentUserService.Email is null)
        {
            return IdentityResponse<CurrentUserResult>.Fail("User is not authenticated.");
        }

        var authStatus = await _identityService.GetUserAuthStatusAsync(_currentUserService.UserId, cancellationToken);

        var result = new CurrentUserResult
        {
            UserId = _currentUserService.UserId,
            Email = _currentUserService.Email,
            Roles = _currentUserService.Roles,
            HasPassword = authStatus.HasPassword,
            AuthProviders = authStatus.AuthProviders
        };

        return IdentityResponse<CurrentUserResult>.Ok(result);
    }
}