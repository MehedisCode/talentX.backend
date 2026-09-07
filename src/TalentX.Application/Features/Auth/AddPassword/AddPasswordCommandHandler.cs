using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.Auth.AddPassword;

public sealed class AddPasswordCommandHandler : IRequestHandler<AddPasswordCommand, Unit>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public AddPasswordCommandHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        ITokenService tokenService)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<Unit> Handle(AddPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        var userId = _currentUserService.UserId;
        var identityResponse = await _identityService.AddPasswordAsync(userId, request.NewPassword, cancellationToken);

        if (!identityResponse.Success)
        {
            if (identityResponse.Errors.Any(error => error.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            {
                throw new NotFoundException("User not found.");
            }

            if (identityResponse.Errors.Any(error => error.Contains("already has a password", StringComparison.OrdinalIgnoreCase)))
            {
                throw new BadRequestException("User already has a password set. Use change password instead.");
            }

            throw new BadRequestException(string.Join(", ", identityResponse.Errors));
        }

        await _tokenService.RevokeOtherSessionsAsync(userId, request.CurrentRefreshToken, cancellationToken);

        return Unit.Value;
    }
}
