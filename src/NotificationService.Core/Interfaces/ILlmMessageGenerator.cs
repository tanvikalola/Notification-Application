using NotificationService.Core.Models;

namespace NotificationService.Core.Interfaces;

public interface ILlmMessageGenerator
{
    Task<string> GenerateAlertMessageAsync(NotificationItem notification, CancellationToken cancellationToken = default);
}
