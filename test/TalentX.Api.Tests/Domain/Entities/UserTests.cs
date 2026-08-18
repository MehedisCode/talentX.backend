using FluentAssertions;
using TalentX.Domain.Entities;

namespace TalentX.Api.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_SetsAllProvidedFields()
    {
        var user = new User("user-1", "Jane Doe", "jane@example.com");

        user.UserId.Should().Be("user-1");
        user.Name.Should().Be("Jane Doe");
        user.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void Update_ChangesNameAndSetsUpdatedAt()
    {
        var user = new User("user-1", "Jane Doe", "jane@example.com");

        var before = DateTime.UtcNow;
        user.Update("Jane Smith");
        var after = DateTime.UtcNow;

        user.Name.Should().Be("Jane Smith");
        user.UpdatedAt.Should().NotBeNull();
        user.UpdatedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Update_DoesNotChangeUserIdOrEmail()
    {
        var user = new User("user-1", "Jane Doe", "jane@example.com");

        user.Update("Jane Smith");

        user.UserId.Should().Be("user-1");
        user.Email.Should().Be("jane@example.com");
    }
}
