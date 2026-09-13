using Microsoft.AspNetCore.Mvc;
using NotificationService.Api;
using NotificationService.Core.Interfaces;
using NotificationService.Core.Logic;
using NotificationService.Core.Models;
using NotificationService.Infrastructure.Configuration;
using NotificationService.Infrastructure.Forwarding;
using NotificationService.Infrastructure.Llm;
using NotificationService.Infrastructure.Queuing;
using NotificationService.Infrastructure.RateLimiting;
using NotificationService.Infrastructure.Tracking;
using NotificationService.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

// Configuration options
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));
builder.Services.Configure<RateLimiterOptions>(builder.Configuration.GetSection(RateLimiterOptions.SectionName));

// Core & Infrastructure services
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISeverityEvaluator, SeverityEvaluator>();
builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
builder.Services.AddSingleton<INotificationQueue, ChannelNotificationQueue>();
builder.Services.AddSingleton<INotificationTracker, InMemoryNotificationTracker>();

// HttpClients
builder.Services.AddHttpClient<ILlmMessageGenerator, OpenAiMessageGenerator>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var timeout = config.GetValue("OpenAi:TimeoutSeconds", 15);
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

builder.Services.AddHttpClient<IAlertForwarder, DiscordAlertForwarder>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var timeout = config.GetValue("Discord:TimeoutSeconds", 15);
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

// Background worker
builder.Services.AddHostedService<NotificationProcessingWorker>();

var app = builder.Build();

app.MapGet("/", (HttpContext context) =>
{
    if (context.Request.Headers.Accept.ToString().Contains("text/html"))
    {
        return Results.Content(DashboardHtml.GetHtml(), "text/html");
    }

    return Results.Ok(new
    {
        service = "Notification Forwarding Service",
        status = "Healthy",
        endpoints = new
        {
            health = "GET /health",
            notifications = "POST /notifications",
            stats = "GET /api/stats"
        },
        rateLimiting = "10 forwarded messages per minute"
    });
});

app.MapGet("/health", (HttpContext context) =>
{
    if (context.Request.Headers.Accept.ToString().Contains("text/html"))
    {
        return Results.Content(DashboardHtml.GetHtml(), "text/html");
    }

    return Results.Ok(new { status = "Healthy" });
});

app.MapGet("/api/stats", (
    [FromServices] INotificationTracker tracker,
    [FromServices] IRateLimiter rateLimiter) =>
{
    var stats = tracker.GetStats();
    return Results.Ok(new
    {
        totalIngested = stats.TotalIngested,
        totalForwarded = stats.TotalForwarded,
        totalFiltered = stats.TotalFiltered,
        currentPermitsUsed = rateLimiter.CurrentPermitsUsed,
        recentEvents = stats.RecentEvents
    });
});

app.MapPost("/notifications", async (
    [FromBody] NotificationRequest? request,
    [FromServices] ISeverityEvaluator severityEvaluator,
    [FromServices] INotificationQueue queue,
    [FromServices] INotificationTracker tracker,
    CancellationToken cancellationToken) =>
{
    if (request is null)
    {
        return Results.BadRequest(new { error = "Request body cannot be null." });
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Missing notification message." });
    }

    if (string.IsNullOrWhiteSpace(request.Level) || !severityEvaluator.TryParse(request.Level, out var severity))
    {
        return Results.BadRequest(new { error = $"Invalid or missing level: '{request.Level}'. Supported levels are info, warning, error, critical." });
    }

    var timestamp = request.Timestamp ?? DateTimeOffset.UtcNow;
    var shouldForward = severityEvaluator.ShouldForward(severity);

    var rawItem = new NotificationItem(
        Level: severity,
        Source: string.IsNullOrWhiteSpace(request.Source) ? "unknown" : request.Source.Trim(),
        Message: request.Message.Trim(),
        Timestamp: timestamp
    );

    var eventId = tracker.RecordIngested(rawItem, shouldForward);

    if (shouldForward)
    {
        var queuedItem = rawItem with { Id = eventId };
        await queue.QueueAsync(queuedItem, cancellationToken);
    }

    return Results.Ok(new NotificationResponse(Accepted: true, Forwarded: shouldForward));
});

app.Run();

public partial class Program { }
