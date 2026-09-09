using MediatR;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Auth.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, IdentityResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IdentityResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return IdentityResponse.Fail("User is not authenticated.");
        }

        var userId = _currentUserService.UserId;

        var changePasswordResult = await _identityService.ChangePasswordAsync(
            userId,
            request.OldPassword,
            request.NewPassword
        );

        if (!changePasswordResult.Success)
        {
            return IdentityResponse.Fail(changePasswordResult.Errors);
        }

        var isGuest = _currentUserService.Roles.Contains(UserRole.Guest.ToString(), StringComparer.OrdinalIgnoreCase);

        if (isGuest)
        {
            var changeRoleResult = await _identityService.ChangeUserRoleAsync(
                userId,
                UserRole.Guest.ToString(),
                UserRole.Talent.ToString()
            );

            if (!changeRoleResult.Success)
            {
                return IdentityResponse.Fail(changeRoleResult.Errors);
            }

            var domainUser = await _unitOfWork.Users.GetByUserIdAsync(userId);
            if (domainUser != null)
            {
                domainUser.UpdateRole(UserRole.Talent);
                if (!domainUser.IsVerified)
                {
                    domainUser.MarkAsVerified();
                }
                _unitOfWork.Users.Update(domainUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return IdentityResponse.Ok();
    }
}
