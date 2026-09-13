using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Core.Interfaces;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Forwarding;
using NotificationService.Infrastructure.Llm;

namespace NotificationService.IntegrationTests.Helpers;

public class NotificationServiceFactory : WebApplicationFactory<Program>
{
    public ConcurrentQueue<HttpRequestMessage> OutboundRequests { get; } = new();
    public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }
    public int MaxMessagesPerMinute { get; set; } = 10;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Configure<OpenAiOptions>(opts =>
            {
                opts.Endpoint = "https://mock-openai.local/v1/chat/completions";
                opts.ApiKey = "mock-openai-key";
                opts.Model = "gpt-4o-mini";
            });

            services.Configure<DiscordOptions>(opts =>
            {
                opts.WebhookUrl = "https://mock-discord.local/api/webhooks/test";
            });

            services.Configure<RateLimiterOptions>(opts =>
            {
                opts.MaxMessagesPerMinute = MaxMessagesPerMinute;
            });

            services.AddHttpClient<ILlmMessageGenerator, OpenAiMessageGenerator>()
                .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(OutboundRequests, ResponseFactory));

            services.AddHttpClient<IAlertForwarder, DiscordAlertForwarder>()
                .ConfigurePrimaryHttpMessageHandler(() => new RecordingHttpMessageHandler(OutboundRequests, ResponseFactory));
        });
    }
}
