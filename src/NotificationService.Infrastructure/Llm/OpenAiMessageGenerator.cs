using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Models;
using NotificationService.Infrastructure.Configuration;

namespace NotificationService.Infrastructure.Llm;

public class OpenAiMessageGenerator : ILlmMessageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiMessageGenerator> _logger;

    public OpenAiMessageGenerator(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiMessageGenerator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAlertMessageAsync(NotificationItem notification, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                _logger.LogWarning("OpenAI endpoint is not configured. Falling back to default message template.");
                return BuildFallbackMessage(notification);
            }

            var requestBody = new ChatCompletionRequest
            {
                Model = _options.Model,
                Temperature = _options.Temperature,
                Messages =
                [
                    new ChatMessage
                    {
                        Role = "system",
                        Content = "You are a concise notification assistant. Summarize the following notification into a short, clear alert message (1-3 sentences) suitable for posting to Discord. Include the severity level, source, and key details."
                    },
                    new ChatMessage
                    {
                        Role = "user",
                        Content = $"Level: {notification.Level.ToString().ToLowerInvariant()}\nSource: {notification.Source}\nMessage: {notification.Message}\nTimestamp: {notification.Timestamp:O}"
                    }
                ]
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = JsonContent.Create(requestBody)
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("OpenAI API call failed with status code {StatusCode}. Details: {Details}. Falling back to templated message.",
                    response.StatusCode, errorDetails);
                return BuildFallbackMessage(notification);
            }

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            var generatedText = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                _logger.LogWarning("OpenAI API returned an empty completion. Falling back to templated message.");
                return BuildFallbackMessage(notification);
            }

            return generatedText;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception during LLM alert message generation. Falling back to templated message.");
            return BuildFallbackMessage(notification);
        }
    }

    public static string BuildFallbackMessage(NotificationItem notification)
    {
        return $"[{notification.Level.ToString().ToLowerInvariant()}] {notification.Source}: {notification.Message}";
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }
}
