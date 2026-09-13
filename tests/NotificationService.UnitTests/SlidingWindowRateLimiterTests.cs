using NotificationService.Infrastructure.RateLimiting;
using NotificationService.UnitTests.Helpers;
using Xunit;

namespace NotificationService.UnitTests;

public class SlidingWindowRateLimiterTests
{
    [Fact]
    public void TryAcquire_Allows10PerMinuteAndBlocksEleventh()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero));
        var rateLimiter = new SlidingWindowRateLimiter(maxPermits: 10, window: TimeSpan.FromMinutes(1), timeProvider);

        // First 10 requests must succeed
        for (int i = 0; i < 10; i++)
        {
            var acquired = rateLimiter.TryAcquire();
            Assert.True(acquired, $"Request {i + 1} should be acquired within limit.");
        }

        // The 11th request must be blocked
        var eleventhAcquired = rateLimiter.TryAcquire();
        Assert.False(eleventhAcquired, "The 11th request within the 1-minute window should be rejected.");
    }

    [Fact]
    public void TryAcquire_PermitsBecomeAvailableAfterWindowPasses()
    {
        var initialTime = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new ManualTimeProvider(initialTime);
        var rateLimiter = new SlidingWindowRateLimiter(maxPermits: 10, window: TimeSpan.FromMinutes(1), timeProvider);

        // Exhaust all 10 permits
        for (int i = 0; i < 10; i++)
        {
            Assert.True(rateLimiter.TryAcquire());
        }
        Assert.False(rateLimiter.TryAcquire());

        // Advance time by 61 seconds (past the 1-minute window)
        timeProvider.Advance(TimeSpan.FromSeconds(61));

        // Permits should be replenished
        for (int i = 0; i < 10; i++)
        {
            Assert.True(rateLimiter.TryAcquire(), $"Request {i + 1} in new window should succeed.");
        }
        Assert.False(rateLimiter.TryAcquire());
    }

    [Fact]
    public void TryAcquire_SlidingWindowPrunesOldRequestsProgressively()
    {
        var initialTime = new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new ManualTimeProvider(initialTime);
        var rateLimiter = new SlidingWindowRateLimiter(maxPermits: 3, window: TimeSpan.FromSeconds(30), timeProvider);

        // t=0s: acquire 2
        Assert.True(rateLimiter.TryAcquire());
        Assert.True(rateLimiter.TryAcquire());

        // t=10s: acquire 1
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        Assert.True(rateLimiter.TryAcquire());

        // t=10s: limit reached (3 in window)
        Assert.False(rateLimiter.TryAcquire());

        // t=31s: the first 2 requests (at t=0) are expired (>30s old), but the 1 at t=10s is still valid (21s old)
        timeProvider.Advance(TimeSpan.FromSeconds(21));

        // We should now be able to acquire 2 more permits
        Assert.True(rateLimiter.TryAcquire());
        Assert.True(rateLimiter.TryAcquire());
        Assert.False(rateLimiter.TryAcquire());
    }

    [Fact]
    public async Task AcquireAsync_WithCancellationToken_CancelsPromptlyWhenBlocked()
    {
        var timeProvider = new ManualTimeProvider();
        var rateLimiter = new SlidingWindowRateLimiter(maxPermits: 1, window: TimeSpan.FromMinutes(1), timeProvider);

        Assert.True(rateLimiter.TryAcquire());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var result = await rateLimiter.AcquireAsync(cts.Token);

        Assert.False(result);
    }
}
