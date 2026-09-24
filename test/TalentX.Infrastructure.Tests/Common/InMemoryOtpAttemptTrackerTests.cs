using TalentX.Application.Common.Constants;
using TalentX.Infrastructure.Common;
using Xunit;

namespace TalentX.Infrastructure.Tests.Common;

public sealed class InMemoryOtpAttemptTrackerTests
{
    [Fact]
    public async Task IsBlockedAsync_ReturnsFalse_BeforeThresholdReached()
    {
        var tracker = new InMemoryOtpAttemptTracker();

        for (var attempt = 0; attempt < OtpAttemptPolicy.MaxFailedAttempts - 1; attempt++)
        {
            await tracker.RecordFailureAsync("key");
        }

        Assert.False(await tracker.IsBlockedAsync("key"));
    }

    [Fact]
    public async Task IsBlockedAsync_ReturnsTrue_AtThreshold()
    {
        var tracker = new InMemoryOtpAttemptTracker();

        for (var attempt = 0; attempt < OtpAttemptPolicy.MaxFailedAttempts; attempt++)
        {
            await tracker.RecordFailureAsync("key");
        }

        Assert.True(await tracker.IsBlockedAsync("key"));
    }

    [Fact]
    public async Task ResetAsync_ClearsAccumulatedFailures()
    {
        var tracker = new InMemoryOtpAttemptTracker();

        for (var attempt = 0; attempt < OtpAttemptPolicy.MaxFailedAttempts; attempt++)
        {
            await tracker.RecordFailureAsync("key");
        }

        await tracker.ResetAsync("key");

        Assert.False(await tracker.IsBlockedAsync("key"));
    }

    [Fact]
    public async Task IsBlockedAsync_IsScopedPerKey()
    {
        var tracker = new InMemoryOtpAttemptTracker();

        for (var attempt = 0; attempt < OtpAttemptPolicy.MaxFailedAttempts; attempt++)
        {
            await tracker.RecordFailureAsync("key-a");
        }

        Assert.True(await tracker.IsBlockedAsync("key-a"));
        Assert.False(await tracker.IsBlockedAsync("key-b"));
    }
}
