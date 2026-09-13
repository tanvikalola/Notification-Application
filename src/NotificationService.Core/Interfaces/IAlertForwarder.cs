namespace NotificationService.Core.Interfaces;

public interface IAlertForwarder
{
    Task ForwardAlertAsync(string message, CancellationToken cancellationToken = default);
}
