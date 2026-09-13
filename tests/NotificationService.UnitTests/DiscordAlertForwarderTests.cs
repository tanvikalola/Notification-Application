using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Forwarding;
using NotificationService.UnitTests.Helpers;
using Xunit;

namespace NotificationService.UnitTests;

public class DiscordAlertForwarderTests
{
    private readonly DiscordOptions _defaultOptions = new()
    {
        WebhookUrl = "https://discord.com/api/webhooks/12345/abcdef"
    };

    [Fact]
    public async Task ForwardAlertAsync_ValidMessage_PostsExactDiscordContentPayload()
    {
        string? capturedBody = null;
        string? capturedUrl = null;

        var handler = new TestHttpMessageHandler(async request =>
        {
            capturedUrl = request.RequestUri?.ToString();
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var httpClient = new HttpClient(handler);
        var forwarder = new DiscordAlertForwarder(httpClient, Options.Create(_defaultOptions), NullLogger<DiscordAlertForwarder>.Instance);

        var alertMessage = "⚠️ Warning from payment-service: repeated timeouts.";
        await forwarder.ForwardAlertAsync(alertMessage);

        Assert.Equal("https://discord.com/api/webhooks/12345/abcdef", capturedUrl);
        Assert.NotNull(capturedBody);

        using var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("content", out var contentProp));
        Assert.Equal(alertMessage, contentProp.GetString());
    }

    [Fact]
    public async Task ForwardAlertAsync_DiscordReturns500_ThrowsHttpRequestException()
    {
        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError, "Discord server error");
        var httpClient = new HttpClient(handler);
        var forwarder = new DiscordAlertForwarder(httpClient, Options.Create(_defaultOptions), NullLogger<DiscordAlertForwarder>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => forwarder.ForwardAlertAsync("Test alert"));
    }

    [Fact]
    public async Task ForwardAlertAsync_MissingWebhookUrl_ThrowsInvalidOperationException()
    {
        var emptyOptions = new DiscordOptions { WebhookUrl = "" };
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, "");
        var httpClient = new HttpClient(handler);
        var forwarder = new DiscordAlertForwarder(httpClient, Options.Create(emptyOptions), NullLogger<DiscordAlertForwarder>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => forwarder.ForwardAlertAsync("Test alert"));
    }
}
