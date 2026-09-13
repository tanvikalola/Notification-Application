using Microsoft.Extensions.Options;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Configuration;

namespace NotificationService.Infrastructure.RateLimiting;

public class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly int _maxPermits;
    private readonly TimeSpan _window;
    private readonly TimeProvider _timeProvider;
    private readonly Queue<DateTimeOffset> _timestamps = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public int MaxPermits => _maxPermits;
    public TimeSpan Window => _window;

    public int CurrentPermitsUsed
    {
        get
        {
            _semaphore.Wait();
            try
            {
                PruneExpiredTimestamps(_timeProvider.GetUtcNow());
                return _timestamps.Count;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public SlidingWindowRateLimiter(
        IOptions<RateLimiterOptions> options,
        TimeProvider? timeProvider = null)
        : this(options.Value.MaxMessagesPerMinute, TimeSpan.FromMinutes(1), timeProvider)
    {
    }

    public SlidingWindowRateLimiter(
        int maxPermits,
        TimeSpan window,
        TimeProvider? timeProvider = null)
    {
        if (maxPermits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPermits), "Max permits must be greater than zero.");
        }

        _maxPermits = maxPermits;
        _window = window;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool TryAcquire()
    {
        _semaphore.Wait();
        try
        {
            var now = _timeProvider.GetUtcNow();
            PruneExpiredTimestamps(now);

            if (_timestamps.Count < _maxPermits)
            {
                _timestamps.Enqueue(now);
                return true;
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan delay = TimeSpan.Zero;
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                var now = _timeProvider.GetUtcNow();
                PruneExpiredTimestamps(now);

                if (_timestamps.Count < _maxPermits)
                {
                    _timestamps.Enqueue(now);
                    return true;
                }

                var oldest = _timestamps.Peek();
                var timeUntilExpiry = (oldest + _window) - now;
                delay = timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : TimeSpan.FromMilliseconds(1);
            }
            finally
            {
                _semaphore.Release();
            }

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            }
        }

        return false;
    }

    private void PruneExpiredTimestamps(DateTimeOffset now)
    {
        var cutoff = now - _window;
        while (_timestamps.Count > 0 && _timestamps.Peek() <= cutoff)
        {
            _timestamps.Dequeue();
        }
    }
}
