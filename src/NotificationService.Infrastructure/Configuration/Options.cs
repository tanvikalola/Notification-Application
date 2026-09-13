namespace NotificationService.Infrastructure.Configuration;

public class OpenAiOptions
{
    public const string SectionName = "OpenAi";

    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.3;
    public int TimeoutSeconds { get; set; } = 15;
}

public class DiscordOptions
{
    public const string SectionName = "Discord";

    public string WebhookUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}

public class RateLimiterOptions
{
    public const string SectionName = "RateLimiting";

    public int MaxMessagesPerMinute { get; set; } = 10;
    public int QueueCapacity { get; set; } = 1000;
}
