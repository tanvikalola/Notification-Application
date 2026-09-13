namespace NotificationService.Core.Interfaces;

public interface IRateLimiter
{
    int MaxPermits { get; }
    TimeSpan Window { get; }
    int CurrentPermitsUsed { get; }

    /// <summary>
    /// Attempts to immediately acquire a permit without waiting.
    /// Returns true if a permit was acquired; false if the rate limit is currently exhausted.
    /// </summary>
    bool TryAcquire();

    /// <summary>
    /// Waits until a permit is available according to the rate limiting policy.
    /// </summary>
    ValueTask<bool> AcquireAsync(CancellationToken cancellationToken = default);
}
