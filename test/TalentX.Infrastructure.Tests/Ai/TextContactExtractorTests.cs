using FluentAssertions;
using TalentX.Infrastructure.Ai.Helpers;
using Xunit;

namespace TalentX.Infrastructure.Tests.Ai;

public class TextContactExtractorTests
{
    [Theory]
    [InlineData("Contact me at john.doe@email.com for job offers", "john.doe@email.com")]
    [InlineData("Email: jane_smith123@domain.co.uk", "jane_smith123@domain.co.uk")]
    [InlineData("No email here in this text", null)]
    public void ExtractEmail_ShouldExtractFirstValidEmail(string text, string? expectedEmail)
    {
        // Act
        var email = TextContactExtractor.ExtractEmail(text);

        // Assert
        email.Should().Be(expectedEmail);
    }

    [Theory]
    [InlineData("Call me at +1 (555) 123-4567 or send SMS", "+1 (555) 123-4567")]
    [InlineData("Phone: 01712345678", "01712345678")]
    [InlineData("Year range 2020-2024 is not phone", null)]
    public void ExtractPhone_ShouldExtractValidPhone(string text, string? expectedPhone)
    {
        // Act
        var phone = TextContactExtractor.ExtractPhone(text);

        // Assert
        phone.Should().Be(expectedPhone);
    }
}
