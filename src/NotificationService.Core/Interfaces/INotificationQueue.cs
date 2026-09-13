using NotificationService.Core.Models;

namespace NotificationService.Core.Interfaces;

public interface INotificationQueue
{
    ValueTask<bool> QueueAsync(NotificationItem item, CancellationToken cancellationToken = default);
    IAsyncEnumerable<NotificationItem> ReadAllAsync(CancellationToken cancellationToken = default);
}
