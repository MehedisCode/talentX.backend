using MediatR;
using Microsoft.AspNetCore.Identity;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Identity.Interfaces;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new BadRequestException("Invalid or expired password reset token.");

        var resetResult = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            throw new BadRequestException("Invalid or expired password reset token.");
        }

        await _tokenService.RevokeAllForUserAsync(user.Id);

        return Unit.Value;
    }
}
