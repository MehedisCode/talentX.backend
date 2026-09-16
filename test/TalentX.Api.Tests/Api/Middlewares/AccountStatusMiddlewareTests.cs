using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Features.Users.Constants;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Middlewares;
using Xunit;

namespace TalentX.Api.Tests.Api.Middlewares;

public class AccountStatusMiddlewareTests
{
    private const string UserId = "identity-user-1";

    private static HttpContext AuthenticatedContext(string userId = UserId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
            authenticationType: "TestAuthType");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsUnauthenticated_CallsNext()
    {
        var userRepository = new Mock<IUserRepository>();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new AccountStatusMiddleware(next);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context, userRepository.Object);

        nextCalled.Should().BeTrue();
        userRepository.Verify(r => r.GetByUserIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsActive_CallsNext()
    {
        var domainUser = new User(UserId, "Jordan Smith", "jordan@talentx.com", UserRole.Talent, isVerified: true);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(domainUser);

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new AccountStatusMiddleware(next);

        await middleware.InvokeAsync(AuthenticatedContext(), userRepository.Object);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsDeactivated_ThrowsForbiddenExceptionAndDoesNotCallNext()
    {
        var domainUser = new User(UserId, "Jordan Smith", "jordan@talentx.com", UserRole.Talent, isVerified: true);
        domainUser.Deactivate();
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(domainUser);

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new AccountStatusMiddleware(next);

        var act = () => middleware.InvokeAsync(AuthenticatedContext(), userRepository.Object);

        var exception = await act.Should().ThrowAsync<ForbiddenException>();
        exception.Which.Message.Should().Be(AccountStatusMessages.Deactivated);
        exception.Which.ResourceId.Should().Be("ACCOUNT_DEACTIVATED");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoMatchingDomainUser_CallsNext()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync((User?)null);

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new AccountStatusMiddleware(next);

        await middleware.InvokeAsync(AuthenticatedContext(), userRepository.Object);

        nextCalled.Should().BeTrue();
    }
}
