using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TalentX.Infrastructure.Identity.Services;
using Xunit;

namespace TalentX.Infrastructure.Tests.Identity.Services;

public sealed class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

    [Fact]
    public void IpAddress_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _httpContextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns((HttpContext?)null);
        var currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = currentUserService.IpAddress;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IpAddress_WhenXForwardedForHeaderPresent_ReturnsFirstTrimmedIp()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = " 203.0.113.195 , 198.51.100.1 ";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");

        _httpContextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns(httpContext);
        var currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = currentUserService.IpAddress;

        // Assert
        result.Should().Be("203.0.113.195");
    }

    [Fact]
    public void IpAddress_WhenXForwardedForHeaderIsSingleIp_ReturnsIp()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "192.168.1.100";

        _httpContextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns(httpContext);
        var currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = currentUserService.IpAddress;

        // Assert
        result.Should().Be("192.168.1.100");
    }

    [Fact]
    public void IpAddress_WhenXForwardedForHeaderIsEmpty_FallsBackToRemoteIpAddress()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "   ";
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        _httpContextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns(httpContext);
        var currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = currentUserService.IpAddress;

        // Assert
        result.Should().Be("127.0.0.1");
    }

    [Fact]
    public void IpAddress_WhenNoHeaderAndRemoteIpAddressSet_ReturnsRemoteIpAddress()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::1");

        _httpContextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns(httpContext);
        var currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = currentUserService.IpAddress;

        // Assert
        result.Should().Be("::1");
    }

    [Fact]
    public void IpAddress_WhenNoHeaderAndRemoteIpAddressNull_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        _httpContextAccessorMock.SetupGet(accessor => accessor.HttpContext).Returns(httpContext);
        var currentUserService = new CurrentUserService(_httpContextAccessorMock.Object);

        // Act
        var result = currentUserService.IpAddress;

        // Assert
        result.Should().BeNull();
    }
}
