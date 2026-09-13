using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Configuration;

namespace NotificationService.Infrastructure.Forwarding;

public class DiscordAlertForwarder : IAlertForwarder
{
    private readonly HttpClient _httpClient;
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordAlertForwarder> _logger;

    public DiscordAlertForwarder(
        HttpClient httpClient,
        IOptions<DiscordOptions> options,
        ILogger<DiscordAlertForwarder> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ForwardAlertAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _logger.LogError("Discord webhook URL is not configured.");
            throw new InvalidOperationException("Discord webhook URL is not configured.");
        }

        var payload = new DiscordWebhookPayload(message);

        using var response = await _httpClient.PostAsJsonAsync(_options.WebhookUrl, payload, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("Failed to forward alert to Discord webhook. Status: {StatusCode}, Error: {Error}",
                response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Successfully forwarded alert to Discord webhook.");
    }

    public sealed record DiscordWebhookPayload(
        [property: JsonPropertyName("content")] string Content
    );
}
