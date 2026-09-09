using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Interfaces;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ResetPasswordWithTokenAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new BadRequestException("Invalid or expired password reset token.");
        }

        var domainUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (domainUser is not null)
        {
            var isModified = false;
            if (domainUser.Role == UserRole.Guest)
            {
                domainUser.UpdateRole(UserRole.Talent);
                isModified = true;
            }

            if (!domainUser.IsVerified)
            {
                domainUser.MarkAsVerified();
                isModified = true;
            }

            if (isModified)
            {
                _unitOfWork.Users.Update(domainUser);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return Unit.Value;
    }
}
