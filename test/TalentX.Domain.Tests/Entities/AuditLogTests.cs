using System.Reflection;
using FluentAssertions;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Domain.Tests.Entities;

public sealed class AuditLogTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly_WhenUserActorProvided()
    {
        // Arrange
        var actor = "admin@example.com";
        var action = "Update · Job";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var auditLog = new AuditLog(actor, action);
        var afterCreation = DateTime.UtcNow;

        // Assert
        auditLog.Id.Should().NotBeEmpty();
        auditLog.Actor.Should().Be(actor);
        auditLog.Action.Should().Be(action);
        auditLog.IsSystem.Should().BeFalse();
        auditLog.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        auditLog.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly_WhenSystemActorProvided()
    {
        // Arrange
        var actor = "System";
        var action = "PurgeExpiredInvitations";
        var beforeCreation = DateTime.UtcNow;

        // Act
        var auditLog = new AuditLog(actor, action, isSystem: true);
        var afterCreation = DateTime.UtcNow;

        // Assert
        auditLog.Id.Should().NotBeEmpty();
        auditLog.Actor.Should().Be(actor);
        auditLog.Action.Should().Be(action);
        auditLog.IsSystem.Should().BeTrue();
        auditLog.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        auditLog.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void ParameterlessConstructor_ShouldBePresentForEntityFrameworkCore()
    {
        // Arrange & Act
        var constructor = typeof(AuditLog).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        // Assert
        constructor.Should().NotBeNull();
    }
}
