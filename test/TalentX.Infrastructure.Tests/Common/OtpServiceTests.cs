using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Infrastructure.Common;
using Xunit;

namespace TalentX.Infrastructure.Tests.Common;

public sealed class OtpServiceTests
{
    private readonly Mock<ILogger<OtpService>> _loggerMock = new();
    private readonly Mock<IHostEnvironment> _environmentMock = new();
    private readonly OtpService _otpService;

    public OtpServiceTests()
    {
        _environmentMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        _otpService = new OtpService(_loggerMock.Object, _environmentMock.Object);
    }

    [Fact]
    public async Task GenerateOtpAsync_GeneratesSixDigitNumericCode()
    {
        var email = "user@example.com";

        var code = await _otpService.GenerateOtpAsync(email);

        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(int.TryParse(code, out _));
    }

    [Fact]
    public async Task VerifyOtpAsync_ReturnsTrue_WhenValidCodeProvided()
    {
        var email = "user@example.com";
        var code = await _otpService.GenerateOtpAsync(email);

        var isValid = await _otpService.VerifyOtpAsync(email, code);

        Assert.True(isValid);
    }

    [Fact]
    public async Task VerifyOtpAsync_ReturnsFalse_WhenCodeMismatch()
    {
        var email = "user@example.com";
        await _otpService.GenerateOtpAsync(email);

        var isValid = await _otpService.VerifyOtpAsync(email, "000000");

        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyOtpAsync_ReturnsFalse_WhenNoOtpGeneratedForEmail()
    {
        var isValid = await _otpService.VerifyOtpAsync("nonexistent@example.com", "123456");

        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyOtpAsync_ReturnsFalse_WhenOtpHasExpired()
    {
        var email = "expired@example.com";
        var code = await _otpService.GenerateOtpAsync(email, TimeSpan.FromMilliseconds(-1));

        var isValid = await _otpService.VerifyOtpAsync(email, code);

        Assert.False(isValid);
    }

    [Fact]
    public async Task VerifyOtpAsync_InvalidatesCodeAfterSuccessfulVerification()
    {
        var email = "user@example.com";
        var code = await _otpService.GenerateOtpAsync(email);

        var firstVerification = await _otpService.VerifyOtpAsync(email, code);
        var secondVerification = await _otpService.VerifyOtpAsync(email, code);

        Assert.True(firstVerification);
        Assert.False(secondVerification);
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_ReturnsTrue_AfterMarkEmailVerifiedAsync()
    {
        var email = "verified@example.com";

        await _otpService.MarkEmailVerifiedAsync(email);
        var isVerified = await _otpService.IsEmailVerifiedAsync(email);

        Assert.True(isVerified);
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_ReturnsFalse_WhenEmailNotMarkedAsVerified()
    {
        var isVerified = await _otpService.IsEmailVerifiedAsync("unverified@example.com");

        Assert.False(isVerified);
    }

    [Fact]
    public async Task IsEmailVerifiedAsync_ReturnsFalse_WhenVerificationExpired()
    {
        var email = "expired-verified@example.com";

        await _otpService.MarkEmailVerifiedAsync(email, TimeSpan.FromMilliseconds(-1));
        var isVerified = await _otpService.IsEmailVerifiedAsync(email);

        Assert.False(isVerified);
    }

    [Fact]
    public async Task ConsumeEmailVerificationAsync_RemovesVerificationState()
    {
        var email = "consumed@example.com";

        await _otpService.MarkEmailVerifiedAsync(email);
        await _otpService.ConsumeEmailVerificationAsync(email);
        var isVerified = await _otpService.IsEmailVerifiedAsync(email);

        Assert.False(isVerified);
    }
}
