using System.Collections.Concurrent;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;

namespace NotificationService.Infrastructure.Tracking;

public class InMemoryNotificationTracker : INotificationTracker
{
    private int _totalIngested;
    private int _totalForwarded;
    private int _totalFiltered;
    private readonly object _lock = new();
    private readonly List<NotificationEventRecord> _events = new();
    private const int MaxEvents = 100;

    public string RecordIngested(NotificationItem item, bool willForward)
    {
        Interlocked.Increment(ref _totalIngested);
        if (willForward)
        {
            // Will increment totalForwarded upon successful worker dispatch
        }
        else
        {
            Interlocked.Increment(ref _totalFiltered);
        }

        var id = string.IsNullOrEmpty(item.Id) ? Guid.NewGuid().ToString("N")[..8] : item.Id;

        var record = new NotificationEventRecord
        {
            Id = id,
            Timestamp = item.Timestamp,
            Level = item.Level.ToString(),
            Source = item.Source,
            Message = item.Message,
            ShouldForward = willForward,
            Status = willForward ? "Enqueued (Awaiting Permit)" : "Filtered (Level < Warning)"
        };

        lock (_lock)
        {
            _events.Insert(0, record);
            if (_events.Count > MaxEvents)
            {
                _events.RemoveAt(_events.Count - 1);
            }
        }

        return id;
    }

    public void RecordForwarded(string id, string alertMessage)
    {
        Interlocked.Increment(ref _totalForwarded);

        lock (_lock)
        {
            var found = _events.FirstOrDefault(e => e.Id == id);
            if (found != null)
            {
                found.Status = "Forwarded to Discord";
                found.GeneratedAlert = alertMessage;
                found.ProcessedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    public void RecordFailed(string id, string error)
    {
        lock (_lock)
        {
            var found = _events.FirstOrDefault(e => e.Id == id);
            if (found != null)
            {
                found.Status = $"Failed: {error}";
                found.ProcessedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    public NotificationStats GetStats()
    {
        lock (_lock)
        {
            return new NotificationStats(
                _totalIngested,
                _totalForwarded,
                _totalFiltered,
                _events.ToList()
            );
        }
    }
}
