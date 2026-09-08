using FluentAssertions;
using Moq;
using TalentX.Application.Features.Auth.ValidateResetToken;
using TalentX.Application.Identity.Interfaces;
using Xunit;

namespace TalentX.Application.Tests.Features.Auth.ValidateResetToken;

public sealed class ValidateResetTokenQueryHandlerTests
{
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly ValidateResetTokenQueryHandler _handler;

    public ValidateResetTokenQueryHandlerTests()
    {
        _handler = new ValidateResetTokenQueryHandler(_identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_ShouldReturnIsValidTrue()
    {
        var email = "user@example.com";
        var token = "valid-token";
        var query = new ValidateResetTokenQuery(email, token);

        _identityServiceMock
            .Setup(service => service.ValidatePasswordResetTokenAsync(email, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsValid.Should().BeTrue();
        _identityServiceMock.Verify(
            service => service.ValidatePasswordResetTokenAsync(email, token, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenIsInvalidOrExpired_ShouldReturnIsValidFalse()
    {
        var email = "user@example.com";
        var token = "invalid-or-expired-token";
        var query = new ValidateResetTokenQuery(email, token);

        _identityServiceMock
            .Setup(service => service.ValidatePasswordResetTokenAsync(email, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        _identityServiceMock.Verify(
            service => service.ValidatePasswordResetTokenAsync(email, token, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
