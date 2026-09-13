using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NotificationService.Core.Models;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Llm;
using NotificationService.UnitTests.Helpers;
using Xunit;

namespace NotificationService.UnitTests;

public class OpenAiMessageGeneratorTests
{
    private readonly OpenAiOptions _defaultOptions = new()
    {
        Endpoint = "https://api.openai.com/v1/chat/completions",
        ApiKey = "sk-test-key",
        Model = "gpt-4o-mini"
    };

    [Fact]
    public async Task GenerateAlertMessageAsync_SuccessfulApiResponse_ReturnsGeneratedAlert()
    {
        var jsonResponse = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "⚠️ Warning from payment-service: repeated timeouts connecting to the upstream API."
                    }
                }
            ]
        }
        """;

        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, jsonResponse);
        var httpClient = new HttpClient(handler);
        var generator = new OpenAiMessageGenerator(httpClient, Options.Create(_defaultOptions), NullLogger<OpenAiMessageGenerator>.Instance);

        var notification = new NotificationItem(
            Level: NotificationSeverity.Warning,
            Source: "payment-service",
            Message: "Timeout connecting to upstream API after 3 retries",
            Timestamp: DateTimeOffset.UtcNow
        );

        var result = await generator.GenerateAlertMessageAsync(notification);

        Assert.Equal("⚠️ Warning from payment-service: repeated timeouts connecting to the upstream API.", result);
        Assert.Single(handler.SentRequests);
        var sent = handler.SentRequests[0];
        Assert.Equal("Bearer", sent.Headers.Authorization?.Scheme);
        Assert.Equal("sk-test-key", sent.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GenerateAlertMessageAsync_ApiReturns500InternalServerError_ReturnsFallbackMessage()
    {
        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError, "{\"error\": \"service unavailable\"}");
        var httpClient = new HttpClient(handler);
        var generator = new OpenAiMessageGenerator(httpClient, Options.Create(_defaultOptions), NullLogger<OpenAiMessageGenerator>.Instance);

        var notification = new NotificationItem(
            Level: NotificationSeverity.Error,
            Source: "order-service",
            Message: "Database connection failed",
            Timestamp: DateTimeOffset.UtcNow
        );

        var result = await generator.GenerateAlertMessageAsync(notification);

        var expectedFallback = "[error] order-service: Database connection failed";
        Assert.Equal(expectedFallback, result);
    }

    [Fact]
    public async Task GenerateAlertMessageAsync_ApiReturns429RateLimit_ReturnsFallbackMessage()
    {
        var handler = new TestHttpMessageHandler(HttpStatusCode.TooManyRequests, "{\"error\": \"rate limited\"}");
        var httpClient = new HttpClient(handler);
        var generator = new OpenAiMessageGenerator(httpClient, Options.Create(_defaultOptions), NullLogger<OpenAiMessageGenerator>.Instance);

        var notification = new NotificationItem(
            Level: NotificationSeverity.Critical,
            Source: "auth-service",
            Message: "Certificate expired",
            Timestamp: DateTimeOffset.UtcNow
        );

        var result = await generator.GenerateAlertMessageAsync(notification);

        var expectedFallback = "[critical] auth-service: Certificate expired";
        Assert.Equal(expectedFallback, result);
    }

    [Fact]
    public async Task GenerateAlertMessageAsync_HttpThrowsException_ReturnsFallbackMessage()
    {
        var handler = new TestHttpMessageHandler(_ => throw new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler);
        var generator = new OpenAiMessageGenerator(httpClient, Options.Create(_defaultOptions), NullLogger<OpenAiMessageGenerator>.Instance);

        var notification = new NotificationItem(
            Level: NotificationSeverity.Warning,
            Source: "inventory-service",
            Message: "Low stock alert",
            Timestamp: DateTimeOffset.UtcNow
        );

        var result = await generator.GenerateAlertMessageAsync(notification);

        var expectedFallback = "[warning] inventory-service: Low stock alert";
        Assert.Equal(expectedFallback, result);
    }

    [Fact]
    public async Task GenerateAlertMessageAsync_EmptyResponseChoices_ReturnsFallbackMessage()
    {
        var handler = new TestHttpMessageHandler(HttpStatusCode.OK, "{\"choices\": []}");
        var httpClient = new HttpClient(handler);
        var generator = new OpenAiMessageGenerator(httpClient, Options.Create(_defaultOptions), NullLogger<OpenAiMessageGenerator>.Instance);

        var notification = new NotificationItem(
            Level: NotificationSeverity.Warning,
            Source: "billing-service",
            Message: "Invoice generation delayed",
            Timestamp: DateTimeOffset.UtcNow
        );

        var result = await generator.GenerateAlertMessageAsync(notification);

        var expectedFallback = "[warning] billing-service: Invoice generation delayed";
        Assert.Equal(expectedFallback, result);
    }
}
