using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NotificationService.Core.Models;
using NotificationService.IntegrationTests.Helpers;
using Xunit;

namespace NotificationService.IntegrationTests;

public class NotificationsEndpointTests : IAsyncLifetime
{
    private NotificationServiceFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new NotificationServiceFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PostNotification_InfoLevel_Returns200AcceptedNotForwarded_NoOutboundCalls()
    {
        var request = new NotificationRequest
        {
            Level = "info",
            Source = "auth-service",
            Message = "User logged in successfully"
        };

        var response = await _client.PostAsJsonAsync("/notifications", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        Assert.NotNull(body);
        Assert.True(body.Accepted);
        Assert.False(body.Forwarded);

        // Wait briefly to confirm background worker did not perform any outbound calls
        await Task.Delay(200);
        Assert.Empty(_factory.OutboundRequests);
    }

    [Theory]
    [InlineData("warning")]
    [InlineData("error")]
    [InlineData("critical")]
    [InlineData("WARNING")]
    [InlineData("Error")]
    [InlineData("CRITICAL")]
    public async Task PostNotification_WarningOrHigher_Returns200AcceptedAndForwarded_OutboundCallMade(string level)
    {
        var request = new NotificationRequest
        {
            Level = level,
            Source = "payment-service",
            Message = "Connection timeout to payment gateway"
        };

        var response = await _client.PostAsJsonAsync("/notifications", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<NotificationResponse>();
        Assert.NotNull(body);
        Assert.True(body.Accepted);
        Assert.True(body.Forwarded);

        // Wait for background worker to process and make outbound calls
        var timeout = TimeSpan.FromSeconds(3);
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            var discordRequests = _factory.OutboundRequests
                .Where(r => r.RequestUri?.ToString().Contains("webhooks") == true)
                .ToList();

            if (discordRequests.Count > 0)
            {
                break;
            }

            await Task.Delay(50);
        }

        var outboundDiscord = _factory.OutboundRequests
            .Where(r => r.RequestUri?.ToString().Contains("webhooks") == true)
            .ToList();

        Assert.NotEmpty(outboundDiscord);
    }

    [Theory]
    [InlineData(null, "some message", "Missing level")]
    [InlineData("", "some message", "Empty level")]
    [InlineData("   ", "some message", "Whitespace level")]
    [InlineData("invalid-level", "some message", "Unknown level")]
    [InlineData("debug", "some message", "Unsupported level debug")]
    [InlineData("trace", "some message", "Unsupported level trace")]
    [InlineData("warning", null, "Missing message")]
    [InlineData("warning", "", "Empty message")]
    [InlineData("warning", "   ", "Whitespace message")]
    public async Task PostNotification_InvalidPayload_Returns400BadRequest(string? level, string? message, string scenario)
    {
        var request = new NotificationRequest
        {
            Level = level,
            Source = "test-service",
            Message = message
        };

        var response = await _client.PostAsJsonAsync("/notifications", request);

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 Bad Request for scenario: {scenario}, but got {response.StatusCode}");
    }

    [Fact]
    public async Task PostNotification_NullBody_Returns400BadRequest()
    {
        var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/notifications", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BurstOf15Requests_AllAccepted200_RateLimiterEnforcesMax10ForwardedPerMinute()
    {
        // Burst of 15 warning notifications sent simultaneously
        var tasks = Enumerable.Range(1, 15).Select(async i =>
        {
            var req = new NotificationRequest
            {
                Level = "warning",
                Source = $"service-{i}",
                Message = $"Burst warning event {i}"
            };
            return await _client.PostAsJsonAsync("/notifications", req);
        });

        var responses = await Task.WhenAll(tasks);

        // All 15 requests are accepted immediately by the API (fire-and-forget queuing)
        foreach (var resp in responses)
        {
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var body = await resp.Content.ReadFromJsonAsync<NotificationResponse>();
            Assert.NotNull(body);
            Assert.True(body.Accepted);
            Assert.True(body.Forwarded);
        }

        // Wait a short duration (1.5s) for the background worker to dispatch initial batch
        await Task.Delay(1500);

        // Within the 1-minute window, exactly 10 Discord webhook messages should have been forwarded
        var discordRequests = _factory.OutboundRequests
            .Where(r => r.RequestUri?.ToString().Contains("webhooks") == true)
            .ToList();

        // Must not exceed the 10 messages/minute limit!
        Assert.True(discordRequests.Count <= 10,
            $"Expected at most 10 Discord webhook calls within 1 minute, but observed {discordRequests.Count}");
        Assert.Equal(10, discordRequests.Count);
    }
}
