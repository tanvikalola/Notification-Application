using System.Text.Json.Serialization;

namespace NotificationService.Core.Models;

public record NotificationRequest
{
    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }
}

public record NotificationResponse(
    [property: JsonPropertyName("accepted")] bool Accepted,
    [property: JsonPropertyName("forwarded")] bool Forwarded
);

public record NotificationItem(
    NotificationSeverity Level,
    string Source,
    string Message,
    DateTimeOffset Timestamp,
    string Id = ""
);
