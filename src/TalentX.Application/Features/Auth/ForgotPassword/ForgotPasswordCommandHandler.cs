using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.Identity.Models;

namespace TalentX.Application.Features.Auth.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new ForgotPasswordResult(true, null, "If an account exists, instructions have been sent.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        var logins = await _userManager.GetLoginsAsync(user);

        if (!hasPassword && logins.Any(login => login.LoginProvider.Equals("Google", StringComparison.OrdinalIgnoreCase)))
        {
            return new ForgotPasswordResult(false, "Google", "This account is registered with Google. Please sign in with Google.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var frontendUrl = _configuration["Frontend:Url"] ?? _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var resetUrl = $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email!)}";

        await _emailService.SendPasswordResetEmailAsync(user.Email!, user.UserName ?? "User", resetUrl, cancellationToken);

        return new ForgotPasswordResult(true, "Local", "If an account exists, instructions have been sent.");
    }
}
