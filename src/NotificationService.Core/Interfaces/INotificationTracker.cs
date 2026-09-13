using NotificationService.Core.Models;

namespace NotificationService.Core.Interfaces;

public record NotificationEventRecord
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string Level { get; init; }
    public required string Source { get; init; }
    public required string Message { get; init; }
    public required bool ShouldForward { get; init; }
    public string Status { get; set; } = "Received";
    public string? GeneratedAlert { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public record NotificationStats(
    int TotalIngested,
    int TotalForwarded,
    int TotalFiltered,
    IReadOnlyList<NotificationEventRecord> RecentEvents
);

public interface INotificationTracker
{
    string RecordIngested(NotificationItem item, bool willForward);
    void RecordForwarded(string id, string alertMessage);
    void RecordFailed(string id, string error);
    NotificationStats GetStats();
}
