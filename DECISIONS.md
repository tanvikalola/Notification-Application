# Design & Implementation Decisions

This document outlines key technical and architectural decisions made while implementing the **Notification Forwarding Service**.

---

## 1. Rate Limiting and Excess Request Strategy

- **Chosen Approach**: **Bounded Queue + Background Worker (Queue for Next Available Slot)**
- **Rationale**:
  - The requirement specified that queueing for the next available slot is preferred (`bounded background queue + BackgroundService`).
  - Dropping critical or warning alerts during brief traffic spikes could lead to missed production incidents.
  - An in-memory bounded queue (`System.Threading.Channels.Channel<NotificationItem>`) provides backpressure, thread safety, and zero external infrastructure dependencies.
  - The API endpoint immediately validates and enqueues warning/error/critical notifications, returning `200 OK` with `{ "accepted": true, "forwarded": true }` to the caller without blocking on external HTTP calls.
  - The `NotificationProcessingWorker` dequeues items and awaits permits from `SlidingWindowRateLimiter` (`AcquireAsync`), strictly ensuring that no more than 10 messages are forwarded per 60-second window.

---

## 2. Severity Evaluation and Thresholding

- **Ordering**: `info (0) < warning (1) < error (2) < critical (3)`
- **Forwarding Rule**: Forward if and only if `level >= warning` (`warning`, `error`, `critical`).
- **Parsing**: Case-insensitive matching (`info`, `warning`, `error`, `critical`).
- **Validation**: Requests with missing, blank, or unsupported severity levels (e.g., `debug`, `trace`, `fatal`) reject immediately with `400 Bad Request`.
- **Message Validation**: Empty or whitespace-only messages reject with `400 Bad Request`.
- **Defaults**: If `timestamp` is omitted in the request body, the server assigns `DateTimeOffset.UtcNow`. If `source` is blank, it defaults to `"unknown"`.

---

## 3. LLM Client Architecture and Fallback Strategy

- **Interface**: `ILlmMessageGenerator` with implementation `OpenAiMessageGenerator`.
- **OpenAI-Compatible Spec**: Uses standard chat completions payload (`/v1/chat/completions`) with `model`, `temperature`, and `messages` array (`system` prompt and `user` payload).
- **Flexibility**: Fully configurable via `appsettings.json` / environment variables (`OpenAi:Endpoint`, `OpenAi:ApiKey`, `OpenAi:Model`), allowing seamless swapping between OpenAI models (`gpt-4o-mini`, `gpt-5.4`, etc.) and compatible providers (Azure OpenAI, Ollama, vLLM, Groq).
- **Graceful Degradation**: If the LLM call times out, encounters a network failure, returns a non-200 status code, or yields an empty completion, it logs a warning and immediately falls back to the deterministic format:
  ```
  [{level}] {source}: {message}
  ```
  This ensures Discord alerts are always dispatched even if third-party AI APIs experience an outage.

---

## 4. Discord Forwarding

- **Interface**: `IAlertForwarder` with implementation `DiscordAlertForwarder`.
- **Payload Shape**: Discord webhook JSON payload: `{"content": "..."}`.
- **Error Handling**: Throws on HTTP error statuses so the worker logs the error with details; verifies the webhook URL is present in configuration.

---

## 5. Testability and Mocking Strategy

- **Zero Unnecessary NuGet Packages**: Avoided heavy mocking frameworks by leveraging .NET 8 BCL abstractions:
  - Subclassed `HttpMessageHandler` (`TestHttpMessageHandler` and `RecordingHttpMessageHandler`) to mock outbound HTTP calls deterministically.
  - Used `System.TimeProvider` and `ManualTimeProvider` for instantaneous testing of sliding-window time progression without real-time delays.
  - End-to-end integration tests use `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) with custom handlers swapped into `IHttpClientFactory`.
