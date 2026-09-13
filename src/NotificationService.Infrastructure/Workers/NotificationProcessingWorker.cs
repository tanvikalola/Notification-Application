using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;

namespace NotificationService.Infrastructure.Workers;

public class NotificationProcessingWorker : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILlmMessageGenerator _llmGenerator;
    private readonly IAlertForwarder _alertForwarder;
    private readonly ILogger<NotificationProcessingWorker> _logger;
    private readonly INotificationTracker? _tracker;

    public NotificationProcessingWorker(
        INotificationQueue queue,
        IRateLimiter rateLimiter,
        ILlmMessageGenerator llmGenerator,
        IAlertForwarder alertForwarder,
        ILogger<NotificationProcessingWorker> logger,
        INotificationTracker? tracker = null)
    {
        _queue = queue;
        _rateLimiter = rateLimiter;
        _llmGenerator = llmGenerator;
        _alertForwarder = alertForwarder;
        _logger = logger;
        _tracker = tracker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification processing background worker started.");

        try
        {
            await foreach (var item in _queue.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    _logger.LogDebug("Processing queued notification from source {Source} with level {Level}.", item.Source, item.Level);

                    // Await slot from rate limiter (sliding window max 10/min)
                    var acquired = await _rateLimiter.AcquireAsync(stoppingToken).ConfigureAwait(false);
                    if (!acquired)
                    {
                        _logger.LogWarning("Rate limiter permit acquisition was cancelled or failed for item from {Source}.", item.Source);
                        _tracker?.RecordFailed(item.Id, "Rate limiter permit acquisition cancelled");
                        continue;
                    }

                    // Generate LLM alert message (with automatic fallback on error)
                    var alertMessage = await _llmGenerator.GenerateAlertMessageAsync(item, stoppingToken).ConfigureAwait(false);

                    // Forward alert to Discord
                    await _alertForwarder.ForwardAlertAsync(alertMessage, stoppingToken).ConfigureAwait(false);

                    _tracker?.RecordForwarded(item.Id, alertMessage);
                    _logger.LogInformation("Notification from {Source} successfully processed and forwarded.", item.Source);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing notification from source {Source}.", item.Source);
                    _tracker?.RecordFailed(item.Id, ex.Message);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Notification processing worker cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Notification processing worker failed unexpectedly.");
        }
    }
}
