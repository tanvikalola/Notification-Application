



# Notification Forwarding Service

A lightweight, reliable ASP.NET Core (.NET 8) Minimal API that receives system notifications, filters them by severity level, summarizes high-severity alerts using an LLM, and forwards them to a Discord webhook with strict rate limiting.

---

## What The Application Does

1. **Receives Notifications**: Exposes `POST /notifications` to ingest incoming alerts.
2. **Evaluates Severity**: Compares severity levels (`info` < `warning` < `error` < `critical`).
   - `info` notifications return `200 OK` (`accepted: true`, `forwarded: false`) and are not forwarded.
   - `warning`, `error`, and `critical` notifications return `200 OK` (`accepted: true`, `forwarded: true`) and are queued for forwarding.
3. **Generates Human-Readable Alerts**: Uses an OpenAI-compatible Chat Completions LLM endpoint to craft a concise 1–3 sentence alert message suitable for Discord. If the LLM call fails or times out, it falls back to a formatted template (`[{level}] {source}: {message}`).
4. **Enforces Rate Limiting**: Ensures that at most **10 messages per minute** are sent to Discord. Excess requests are queued in-memory and processed as slots become available.
5. **Non-Blocking Ingestion**: The HTTP endpoint accepts notifications and acknowledges them immediately using an in-memory bounded queue (`System.Threading.Channels`), decoupling HTTP ingress latency from external API response times.

---

## Architecture Overview

```
                          ┌───────────────────────────┐
                          │   Client / Ingress API    │
                          └─────────────┬─────────────┘
                                        │ HTTP POST /notifications
                                        ▼
                        ┌───────────────────────────────┐
                        │   NotificationService.Api     │
                        │    (Validation & Severity)    │
                        └───────┬───────────────┬───────┘
                     level < warning     level >= warning
                                │               │
                        ┌───────▼──────┐ ┌──────▼─────────────────────┐
                        │ 200 Not Fwd  │ │ ChannelNotificationQueue   │
                        └──────────────┘ └──────────────┬─────────────┘
                                                        │ ReadAllAsync
                                         ┌──────────────▼─────────────┐
                                         │ NotificationProcessingWorker│
                                         └──────────────┬─────────────┘
                                                        │
                                         ┌──────────────▼─────────────┐
                                         │  SlidingWindowRateLimiter  │
                                         │    (Max 10 permits/min)    │
                                         └──────────────┬─────────────┘
                                                        │ Permit Acquired
                                         ┌──────────────▼─────────────┐
                                         │   ILlmMessageGenerator     │
                                         │  (OpenAI + Fallback Temp)  │
                                         └──────────────┬─────────────┘
                                                        │ Formatted Alert
                                         ┌──────────────▼─────────────┐
                                         │    DiscordAlertForwarder   │
                                         │      (Webhook POST)        │
                                         └──────────────┬─────────────┘
                                                        │
                                                        ▼
                                                  Discord Channel
```

### Component Breakdown
- **`NotificationService.Core`**: Domain models (`NotificationRequest`, `NotificationResponse`, `NotificationItem`), severity classification logic (`SeverityEvaluator`), and service interfaces (`ILlmMessageGenerator`, `IAlertForwarder`, `IRateLimiter`, `INotificationQueue`).
- **`NotificationService.Infrastructure`**: Concrete implementations for OpenAI chat completions (`OpenAiMessageGenerator`), Discord webhook publishing (`DiscordAlertForwarder`), thread-safe sliding window rate limiting (`SlidingWindowRateLimiter`), in-memory bounded channel (`ChannelNotificationQueue`), and background processing (`NotificationProcessingWorker`).
- **`NotificationService.Api`**: Minimal API endpoints, configuration binding, and dependency injection setup.
- **`NotificationService.UnitTests`**: Unit tests verifying severity ranking, sliding window enforcement, LLM fallbacks, and Discord payload structure.
- **`NotificationService.IntegrationTests`**: End-to-end integration tests using `WebApplicationFactory<Program>` with recorded HTTP mocking.

---

## Configuration

Configuration values are read from `appsettings.json` or standard environment variables:

```json
{
  "OpenAi": {
    "Endpoint": "https://api.openai.com/v1/chat/completions",
    "ApiKey": "YOUR_OPENAI_API_KEY",
    "Model": "gpt-4o-mini",
    "Temperature": 0.3,
    "TimeoutSeconds": 15
  },
  "Discord": {
    "WebhookUrl": "https://discord.com/api/webhooks/YOUR/WEBHOOK/URL",
    "TimeoutSeconds": 15
  },
  "RateLimiting": {
    "MaxMessagesPerMinute": 10,
    "QueueCapacity": 1000
  }
}
```

### Environment Variable Overrides
- `OpenAi__ApiKey`: Secret API key for the LLM provider.
- `OpenAi__Endpoint`: Any OpenAI-compatible chat completions endpoint (OpenAI, Azure, Groq, Ollama, etc.).
- `OpenAi__Model`: Model string (e.g. `gpt-4o-mini`, `gpt-4o`, `gpt-5.4`).
- `Discord__WebhookUrl`: Target Discord webhook URL.
- `RateLimiting__MaxMessagesPerMinute`: Maximum outbound forwarded messages allowed in any 60-second window (default: `10`).

---

## How to Run Locally

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run Application
```powershell
dotnet run --project src/NotificationService.Api/NotificationService.Api.csproj
```
The service will start listening on `http://localhost:5000` (or the configured launch URL).

Health check:
```powershell
curl http://localhost:5000/health
```

---

## How to Run Tests

Run all unit and integration tests across the solution:
```powershell
dotnet test NotificationService.sln
```

Run unit tests only:
```powershell
dotnet test tests/NotificationService.UnitTests/NotificationService.UnitTests.csproj
```

Run integration tests only:
```powershell
dotnet test tests/NotificationService.IntegrationTests/NotificationService.IntegrationTests.csproj
```

---

## API Contract

### Ingest Notification
`POST /notifications`

#### Headers
`Content-Type: application/json`

#### Request Body
```json
{
  "level": "warning",
  "source": "payment-service",
  "message": "Timeout connecting to upstream API after 3 retries",
  "timestamp": "2026-09-11T10:15:00Z"
}
```

- `level` *(string, required)*: `"info"`, `"warning"`, `"error"`, or `"critical"` (case-insensitive).
- `source` *(string, optional)*: Identifier for the originating service or component. Defaults to `"unknown"` if omitted.
- `message` *(string, required)*: Alert message details.
- `timestamp` *(string, optional, ISO 8601)*: Timestamp of the event. Defaults to current server UTC time if absent.

#### Responses

##### Success (Warning / Error / Critical - Queued for Forwarding)
Status: `200 OK`
```json
{
  "accepted": true,
  "forwarded": true
}
```

##### Success (Info - Not Forwarded)
Status: `200 OK`
```json
{
  "accepted": true,
  "forwarded": false
}
```

##### Validation Failure (Missing or Invalid Level / Missing Message)
Status: `400 Bad Request`
```json
{
  "error": "Invalid or missing level: 'debug'. Supported levels are info, warning, error, critical."
}
```

---

## Rate Limiting Behavior

- **Window**: 60-second sliding window log (`SlidingWindowRateLimiter`).
- **Throughput**: Maximum of 10 forwarded messages per minute.
- **Excess Traffic Policy**: **Queueing (Preferred Approach)**.
  - When traffic bursts exceed 10 messages within a 1-minute window, messages are retained in the FIFO bounded channel queue.
  - The background worker awaits the release of the oldest slot in the sliding window (`AcquireAsync`) before dispatching the next message.
  - This prevents notification loss during intermittent incident storms while protecting Discord webhook endpoints from rate-limit violations (HTTP 429).
