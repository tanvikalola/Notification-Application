namespace NotificationService.UnitTests.Helpers;

public class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public ManualTimeProvider(DateTimeOffset? initialTime = null)
    {
        _utcNow = initialTime ?? new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }

    public void SetUtcNow(DateTimeOffset time)
    {
        _utcNow = time;
    }
}
