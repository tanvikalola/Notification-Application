using System.Threading.Channels;
using Microsoft.Extensions.Options;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;
using NotificationService.Infrastructure.Configuration;

namespace NotificationService.Infrastructure.Queuing;

public class ChannelNotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationItem> _channel;

    public ChannelNotificationQueue(IOptions<RateLimiterOptions> options)
    {
        var capacity = options.Value.QueueCapacity > 0 ? options.Value.QueueCapacity : 1000;
        var channelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<NotificationItem>(channelOptions);
    }

    public async ValueTask<bool> QueueAsync(NotificationItem item, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public IAsyncEnumerable<NotificationItem> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
