using System.Reflection;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TalentX.API.Controllers.Auth;
using TalentX.Application.Features.Auth.AddPassword;
using TalentX.Application.Features.Auth.ForgotPassword;
using TalentX.Application.Features.Auth.ResetPassword;
using Xunit;

namespace TalentX.Api.Tests.Controllers.Auth;

public sealed class AuthControllerTests
{
    private readonly Mock<ISender> _senderMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_senderMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        _controller = new AuthController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
            }
        };
    }

    [Fact]
    public void AddPassword_HasAuthorizeAttribute()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.AddPassword));
        var attribute = method?.GetCustomAttribute<AuthorizeAttribute>(inherit: false);

        attribute.Should().NotBeNull();
    }

    [Fact]
    public async Task AddPassword_SendsCommandAndReturnsNoContent()
    {
        var request = new AddPasswordRequestDto("ValidPass123!", "ValidPass123!", "test-refresh-token");

        _senderMock
            .Setup(sender => sender.Send(It.IsAny<AddPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var response = await _controller.AddPassword(request, CancellationToken.None);

        response.Should().BeOfType<NoContentResult>();

        _senderMock.Verify(
            sender => sender.Send(
                It.Is<AddPasswordCommand>(command =>
                    command.NewPassword == request.NewPassword &&
                    command.ConfirmPassword == request.ConfirmPassword &&
                    command.CurrentRefreshToken == request.CurrentRefreshToken),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ForgotPassword_HasAllowAnonymousAttribute()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.ForgotPassword));
        var attribute = method?.GetCustomAttribute<AllowAnonymousAttribute>(inherit: false);

        attribute.Should().NotBeNull();
    }

    [Fact]
    public async Task ForgotPassword_SendsCommandAndReturnsOkWithResult()
    {
        var request = new ForgotPasswordRequestDto("user@example.com");
        var expectedResult = new ForgotPasswordResult(true, "Local", "If an account exists, instructions have been sent.");

        _senderMock
            .Setup(sender => sender.Send(It.IsAny<ForgotPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var response = await _controller.ForgotPassword(request, CancellationToken.None);

        var okResult = response.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedResult);

        _senderMock.Verify(
            sender => sender.Send(
                It.Is<ForgotPasswordCommand>(command => command.Email == request.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ResetPassword_HasAllowAnonymousAttribute()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.ResetPassword));
        var attribute = method?.GetCustomAttribute<AllowAnonymousAttribute>(inherit: false);

        attribute.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPassword_SendsCommandAndReturnsNoContent()
    {
        var request = new ResetPasswordRequestDto("user@example.com", "valid-token", "NewPassword123!", "NewPassword123!");

        _senderMock
            .Setup(sender => sender.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var response = await _controller.ResetPassword(request, CancellationToken.None);

        response.Should().BeOfType<NoContentResult>();

        _senderMock.Verify(
            sender => sender.Send(
                It.Is<ResetPasswordCommand>(command =>
                    command.Email == request.Email &&
                    command.Token == request.Token &&
                    command.NewPassword == request.NewPassword &&
                    command.ConfirmPassword == request.ConfirmPassword),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
