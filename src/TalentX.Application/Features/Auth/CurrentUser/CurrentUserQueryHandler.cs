using MediatR;
using TalentX.Application.Common.Identity.Models;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Application.Features.Auth.CurrentUser;

public class CurrentUserQueryHandler
    : IRequestHandler<CurrentUserQuery, IdentityResponse<CurrentUserResult>>
{
    private readonly ICurrentUserService _currentUserService;

    public CurrentUserQueryHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Task<IdentityResponse<CurrentUserResult>> Handle(
        CurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated
            || _currentUserService.UserId is null
            || _currentUserService.Email is null)
        {
            return Task.FromResult(
                IdentityResponse<CurrentUserResult>.Fail("User is not authenticated."));
        }

        var result = new CurrentUserResult
        {
            UserId = _currentUserService.UserId,
            Email = _currentUserService.Email,
            Roles = _currentUserService.Roles
        };

        return Task.FromResult(IdentityResponse<CurrentUserResult>.Ok(result));
    }
}