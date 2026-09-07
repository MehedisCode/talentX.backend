using FluentAssertions;
using Moq;
using TalentX.Application.Features.Auth.CurrentUser;
using TalentX.Application.Identity.Interfaces;
using TalentX.Application.Identity.Models;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.CurrentUser;

public class CurrentUserQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly CurrentUserQueryHandler _handler;

    public CurrentUserQueryHandlerTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _identityServiceMock = new Mock<IIdentityService>();
        _handler = new CurrentUserQueryHandler(
            _currentUserServiceMock.Object,
            _identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserIsAuthenticated_ShouldReturnCurrentUserResultWithAuthStatus()
    {
        var userId = "user-123";
        var email = "user@example.com";
        var roles = new List<string> { "Employer" };
        var authProviders = new List<string> { "google", "password" };
        var authStatus = new UserAuthStatusResponse(true, authProviders);

        _currentUserServiceMock.Setup(service => service.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(service => service.UserId).Returns(userId);
        _currentUserServiceMock.Setup(service => service.Email).Returns(email);
        _currentUserServiceMock.Setup(service => service.Roles).Returns(roles);

        _identityServiceMock
            .Setup(service => service.GetUserAuthStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authStatus);

        var query = new CurrentUserQuery();

        var response = await _handler.Handle(query, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.UserId.Should().Be(userId);
        response.Data.Email.Should().Be(email);
        response.Data.Roles.Should().BeEquivalentTo(roles);
        response.Data.HasPassword.Should().BeTrue();
        response.Data.AuthProviders.Should().BeEquivalentTo(authProviders);

        _identityServiceMock.Verify(
            service => service.GetUserAuthStatusAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ShouldReturnFailureResponse()
    {
        _currentUserServiceMock.Setup(service => service.IsAuthenticated).Returns(false);

        var query = new CurrentUserQuery();

        var response = await _handler.Handle(query, CancellationToken.None);

        response.Success.Should().BeFalse();
        response.Errors.Should().Contain("User is not authenticated.");
        _identityServiceMock.Verify(
            service => service.GetUserAuthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGoogleOnlyUser_ShouldReturnHasPasswordFalseAndGoogleProvider()
    {
        var userId = "google-user-456";
        var email = "googleuser@example.com";
        var roles = new List<string> { "Talent" };
        var authProviders = new List<string> { "google" };
        var authStatus = new UserAuthStatusResponse(false, authProviders);

        _currentUserServiceMock.Setup(service => service.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(service => service.UserId).Returns(userId);
        _currentUserServiceMock.Setup(service => service.Email).Returns(email);
        _currentUserServiceMock.Setup(service => service.Roles).Returns(roles);

        _identityServiceMock
            .Setup(service => service.GetUserAuthStatusAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authStatus);

        var query = new CurrentUserQuery();

        var response = await _handler.Handle(query, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.HasPassword.Should().BeFalse();
        response.Data.AuthProviders.Should().Equal("google");
    }
}
