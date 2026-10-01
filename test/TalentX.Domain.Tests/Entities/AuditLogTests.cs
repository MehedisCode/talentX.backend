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
        auditLog.IpAddress.Should().BeNull();
        auditLog.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        auditLog.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldInitializeIpAddress_WhenIpAddressProvided()
    {
        // Arrange
        const string actor = "admin@example.com";
        const string action = "Update · Job";
        const string ipAddress = "192.168.1.100";

        // Act
        var auditLog = new AuditLog(actor, action, isSystem: false, ipAddress: ipAddress);

        // Assert
        auditLog.IpAddress.Should().Be(ipAddress);
    }

    [Fact]
    public void Constructor_ShouldDefaultIpAddressToNull_WhenNotProvided()
    {
        // Arrange
        const string actor = "admin@example.com";
        const string action = "Insert · Job";

        // Act
        var auditLog = new AuditLog(actor, action);

        // Assert
        auditLog.IpAddress.Should().BeNull();
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
