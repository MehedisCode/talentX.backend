using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Users.Constants;
using TalentX.Domain.Enums;

namespace TalentX.Infrastructure.Middlewares;

public sealed class AccountStatusMiddleware
{
    private readonly RequestDelegate _next;

    public AccountStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                var user = await userRepository.GetByUserIdAsync(userId);
                if (user is not null && user.Status == AccountStatus.Deactivated)
                {
                    throw new ForbiddenException(AccountStatusMessages.Deactivated, resourceId: "ACCOUNT_DEACTIVATED");
                }
            }
        }

        await _next(context);
    }
}
