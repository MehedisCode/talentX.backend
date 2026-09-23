using FluentAssertions;
using TalentX.Infrastructure.Common;
using Xunit;

namespace TalentX.Infrastructure.Tests.Common;

public sealed class InMemoryOtpCooldownTrackerTests
{
    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
    }

    [Fact]
    public async Task IsOnCooldownAsync_ReturnsFalse_WhenNotSet()
    {
        var tracker = new InMemoryOtpCooldownTracker();

        var result = await tracker.IsOnCooldownAsync("test-key");

        result.Should().BeFalse();
        (await tracker.GetRemainingCooldownSecondsAsync("test-key")).Should().Be(0);
    }

    [Fact]
    public async Task IsOnCooldownAsync_ReturnsTrue_DuringCooldown()
    {
        var timeProvider = new TestTimeProvider();
        var tracker = new InMemoryOtpCooldownTracker(timeProvider);

        await tracker.SetCooldownAsync("test-key", TimeSpan.FromSeconds(60));

        (await tracker.IsOnCooldownAsync("test-key")).Should().BeTrue();
        (await tracker.GetRemainingCooldownSecondsAsync("test-key")).Should().Be(60);

        timeProvider.Advance(TimeSpan.FromSeconds(20));

        (await tracker.IsOnCooldownAsync("test-key")).Should().BeTrue();
        (await tracker.GetRemainingCooldownSecondsAsync("test-key")).Should().Be(40);
    }

    [Fact]
    public async Task IsOnCooldownAsync_ReturnsFalse_AfterExpiry()
    {
        var timeProvider = new TestTimeProvider();
        var tracker = new InMemoryOtpCooldownTracker(timeProvider);

        await tracker.SetCooldownAsync("test-key", TimeSpan.FromSeconds(60));

        timeProvider.Advance(TimeSpan.FromSeconds(61));

        (await tracker.IsOnCooldownAsync("test-key")).Should().BeFalse();
        (await tracker.GetRemainingCooldownSecondsAsync("test-key")).Should().Be(0);
    }

    [Fact]
    public async Task ClearCooldownAsync_RemovesActiveCooldown()
    {
        var tracker = new InMemoryOtpCooldownTracker();

        await tracker.SetCooldownAsync("test-key", TimeSpan.FromSeconds(60));
        (await tracker.IsOnCooldownAsync("test-key")).Should().BeTrue();

        await tracker.ClearCooldownAsync("test-key");

        (await tracker.IsOnCooldownAsync("test-key")).Should().BeFalse();
        (await tracker.GetRemainingCooldownSecondsAsync("test-key")).Should().Be(0);
    }

    [Fact]
    public async Task Cooldown_IsKeySpecific()
    {
        var tracker = new InMemoryOtpCooldownTracker();

        await tracker.SetCooldownAsync("key-1", TimeSpan.FromSeconds(60));

        (await tracker.IsOnCooldownAsync("key-1")).Should().BeTrue();
        (await tracker.IsOnCooldownAsync("key-2")).Should().BeFalse();
    }
}
