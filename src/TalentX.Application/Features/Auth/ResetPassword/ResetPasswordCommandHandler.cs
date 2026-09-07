using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Identity.Interfaces;

namespace TalentX.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
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

        return Unit.Value;
    }
}
