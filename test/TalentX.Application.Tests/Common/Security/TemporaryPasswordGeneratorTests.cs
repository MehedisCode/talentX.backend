using FluentAssertions;
using TalentX.Application.Common.Security;
using Xunit;

namespace TalentX.Application.Tests.Common.Security;

public class TemporaryPasswordGeneratorTests
{
    [Fact]
    public void Generate_DefaultsToTwelveCharacters()
    {
        var password = TemporaryPasswordGenerator.Generate();

        password.Length.Should().Be(12);
    }

    [Fact]
    public void Generate_MeetsIdentityPasswordPolicy()
    {
        for (var i = 0; i < 50; i++)
        {
            var password = TemporaryPasswordGenerator.Generate();

            password.Length.Should().Be(12);
            password.Should().MatchRegex("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{12}$");
        }
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(20)]
    public void Generate_WithSupportedLength_ReturnsPasswordOfThatLength(int length)
    {
        var password = TemporaryPasswordGenerator.Generate(length);

        password.Length.Should().Be(length);
        password.Should().MatchRegex("^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d)(?=.*[^A-Za-z0-9]).+$");
    }

    [Fact]
    public void Generate_WithLengthBelowPolicyMinimum_Throws()
    {
        var act = () => TemporaryPasswordGenerator.Generate(7);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_ExcludesAmbiguousCharacters()
    {
        for (var i = 0; i < 50; i++)
        {
            var password = TemporaryPasswordGenerator.Generate();

            password.Should().NotContain("0");
            password.Should().NotContain("O");
            password.Should().NotContain("1");
            password.Should().NotContain("l");
        }
    }

    [Fact]
    public void Generate_ProducesUniquePasswords()
    {
        var generated = Enumerable.Range(0, 1000)
            .Select(_ => TemporaryPasswordGenerator.Generate())
            .ToList();

        generated.Distinct().Count().Should().Be(generated.Count);
    }
}