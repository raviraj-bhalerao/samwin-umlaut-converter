# Developer Guide

## 1. Introduction

This guide provides practical instructions for extending and maintaining the **Samwin Umlaut Converter API**.
It defines conventions and patterns to ensure consistency, observability, and maintainability.

---

## 2. Observability (Logs, Traces, Metrics)

### 2.1 Logging

* Use **structured logging** via `ILogger`
* Avoid string concatenation; use placeholders
* Always log meaningful events (start, success, failure)

```csharp
_logger.LogInformation("Processing request for {InputCount} inputs", inputs.Length);
```

---

### 2.2 Logging Scopes

Use scopes to enrich logs with context:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    { "Scope", "OperationName" },
    { "OperationId", Guid.NewGuid() }
}))
{
    // logic
}
```

**Standard fields:**

* `Scope` → Logical operation name
* `OperationId` → Unique per request
* Additional contextual fields (e.g., input count)

---

## 2.3 Tracing

Use `IActivityService` to create traces:

```csharp
using (_activityService.StartActivity("OperationName"))
{
    // traced logic
}
```

---

### Guidelines:

* Use meaningful names:

  * `PublishMessage`
  * `ReceiveMessage`
  * `GenerateJwt`
* Wrap:

  * external calls
  * expensive operations
  * business-critical logic

---

### Distributed Tracing (RabbitMQ)

The system supports **automatic trace propagation across RabbitMQ messaging**.

You do NOT need to manually handle propagation in application code.

Handled internally by:

* `TextMapPropagator`
* `MessageBusClient`

This ensures:

* Producer and consumer share the same trace
* Async workflows remain correlated
* End-to-end observability across services

---

# 2.4 Metrics

Use `MetricsService` for all application metrics.

Metrics provide insight into **system behavior, performance, and efficiency**.

---

## 2.4.1 Metric Types

### Counters

Used for **monotonically increasing values** (event counts).

```csharp
_metricsService.TokensConverted.Add(1);
```

---

### Histograms

Used for **durations and distributions**.

```csharp
_metricsService.JwtGenerationTime.Record(duration);
```

---

## 2.4.2 Request Metrics (Middleware-Based)

Request metrics are handled centrally via `MetricsMiddleware`.

### Behavior:

* Automatically records **one metric per HTTP request**
* Captures:

  * `endpoint` → resolved from endpoint metadata
  * `status_code` → HTTP response status
* Executed after request processing (including failures)

---

### Implementation:

```csharp
app.UseMiddleware<MetricsMiddleware>();
```

---

### Metric Structure:

```csharp
_metricsService.RequestCounter.Add(1, new TagList
{
    { "endpoint", endpointName },
    { "status_code", context.Response.StatusCode }
});
```

---

### Guidelines:

* ❌ Do NOT add request counters inside controllers
* ❌ Do NOT duplicate request metrics in services
* ✔ Request counting is handled **only via middleware**
* ✔ Ensures consistent and complete coverage across all endpoints

---

### Endpoint Naming:

* Use `[EndpointName("MeaningfulName")]` when clarity is needed
* Otherwise, `DisplayName` is used as fallback

---

### Design Principle:

> Cross-cutting concerns like request metrics must be handled centrally, not in business logic.

---

## 2.4.3 Cache Metrics (Hit / Miss Guidelines)

Cache metrics measure **system efficiency and load reduction**.

---

### Definitions:

* **Cache Hit** → Data served from cache (no external processing)
* **Cache Miss** → Data not in cache (requires queue / processing)

---

### Usage Guidelines:

* Record **exactly one event per evaluated input**
* A single input must result in either:

  * `CacheHit`
  * `CacheMiss`
* Never record both for the same input

---

### Example:

```csharp
if (_cache.TryGetValue(key, out var value))
{
    _metricsService.RecordCacheHit();
}
else
{
    _metricsService.RecordCacheMiss();
}
```

---

### Important Rules:

* Record metrics **at decision point**, not after processing
* Do NOT record multiple times for the same input
* Metrics must reflect **decision**, not outcome

---

### Interpretation:

```
Cache Hit Ratio = CacheHits / (CacheHits + CacheMisses)
```

---

### Design Notes:

* Metrics are recorded per **unique input**
* When using deduplication (`Distinct()`), metrics reflect unique values, not total request size

---

## 2.4.4 Counter Design Guidelines

Counters track **event occurrences across the system**.

---

### Naming Conventions:

* Use **snake_case**
* Prefix with service/domain
* Use `_total` suffix for counters

```
umlaut_converter_requests_total
umlaut_converter_cache_hits_total
umlaut_converter_tokens_converted_total
```

---

### When to Use Counters:

Use counters for:

* Requests (via middleware)
* Cache hits/misses
* Messages published/consumed
* Tokens processed
* Errors (recommended)

---

### When NOT to Use Counters:

Do NOT use counters for:

* durations → use **Histogram**
* current values → use **Gauge** (if required)
* percentages → compute externally (e.g., Grafana)

---

### Increment Strategy:

* Prefer increment by `1` per event
* Use higher values only when batching:

```csharp
_metricsService.TokensConverted.Add(tokenCount);
```

---

## 2.4.5 Tagging Guidelines

Tags provide **metric segmentation and filtering**.

---

### Guidelines:

* Use tags only when necessary
* Keep values **low cardinality**

---

### ✔ Good Tags:

* `result = hit / miss`
* `endpoint = convert`
* `status_code = 200 / 500`

---

### ❌ Bad Tags:

* userId
* input value
* requestId
* any high-cardinality or unique value

---

## 2.4.6 Consistency Rules

* Every feature should include:

  * relevant counters
  * optional histogram for performance

* Always use `MetricsService`

* Do NOT create ad-hoc meters or duplicate metrics

---

## 2.4.7 Anti-Patterns to Avoid

❌ Duplicating request metrics in controllers
❌ Missing metrics in new features
❌ High-cardinality tags
❌ Using counters for non-count data

---

### Metric Design Anti-Pattern:

❌ Separate counters:

```
cache_hits
cache_misses
```

✔ Preferred (production):

```
cache_events_total { result = hit / miss }
```

*(Separate counters are acceptable for demo simplicity)*

---

## 2.4.8 Observability Alignment

Every feature should include:

| Concern | Tool                     |
| ------- | ------------------------ |
| Logs    | ILogger                  |
| Traces  | Activity / OpenTelemetry |
| Metrics | Counters / Histograms    |

---


## 3. Adding a New API Endpoint

### Steps:

1. Create a new controller or extend existing one
2. Add logging scope
3. Add tracing activity
4. Add metrics

```csharp
[HttpGet("example")]
public IActionResult Example()
{
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        { "Scope", "Example" },
        { "OperationId", Guid.NewGuid() }
    }))
    using (_activityService.StartActivity("ExampleOperation"))
    {
        _logger.LogInformation("Processing example request");
        return Ok();
    }
}
```

---

## 4. Adding a New Converter / Variation Strategy

### Interface

Implement:

```csharp
IVariationGenerator
```

### Steps:

1. Create a new class implementing the interface
2. Add logic for variation generation
3. Register in DI:

```csharp
services.AddTransient<IVariationGenerator, YourNewGenerator>();
```

### Guidelines:

* Keep implementation stateless
* Optimize for performance (this is a hot path)
* Avoid unnecessary allocations

---

## 5. Adding a New SQL Generator

### Interface

```csharp
ISqlQueryGenerator<T>
```

### Steps:

1. Implement generator
2. Ensure parameterized queries (avoid SQL injection)
3. Register in DI

```csharp
services.AddTransient<ISqlQueryGenerator<SqlQuery>, YourGenerator>();
```

---

## 6. Working with Message Queue (RabbitMQ)

### Publishing Messages

Use:

```csharp
await _messageBusClient.PublishMessageAsync(message);
```

### Consumed Messages

Handled by background consumer service using manual acknowledgment.

---

### Distributed Tracing (IMPORTANT UPDATE)

The system supports **end-to-end distributed tracing across RabbitMQ**.

When publishing messages:

* OpenTelemetry context is injected into message headers
* Trace context is preserved across async boundaries

When consuming messages:

* Trace context is extracted from headers
* Consumer activity is linked as a **child span of the producer**

This enables full traceability across:

> API → RabbitMQ → Consumer → Processing → SSE

---

### Guidelines

* Messages should be idempotent
* Avoid large payloads
* Always handle exceptions
* Ensure trace context is preserved (handled by `MessageBusClient` automatically)


---

## 7. Authentication

### JWT Generation

Handled in:

```
TokenGeneratorService
```

### To modify:

* Token lifetime
* Claims
* Signing key

---

## 8. Authorization

### Role-based Authorization

```csharp
[Authorize(Roles = "Admin")]
```

### Guidelines:

* Use roles for coarse-grained access
* Avoid hardcoding roles in multiple places

---

## 9. Adding Custom Claims to JWT

Modify:

```
TokenGeneratorService
```

### Example:

```csharp
claims.Add(new Claim("department", "engineering"));
```

### Access in Controller:

```csharp
var department = User.FindFirst("department")?.Value;
```

---

## 10. Server-Sent Events (SSE)

### Usage Pattern

* Use `Channel<T>` for buffering
* Stream using `IAsyncEnumerable`

### Example Flow:

* Subscribe to event
* Push to channel
* Yield to client

### Guidelines:

* Always handle client disconnect
* Use timeout (30s pattern already implemented)

---

## 11. Error Handling & Resilience

* Log all exceptions
* Use retry only where necessary
* Queue failures should use **Nack + requeue**

```csharp
await channel.BasicNackAsync(tag, false, true);
```

---

## 12. Configuration Guidelines

### Environment Variables

* Use environment variables for all external configs

### Local Development

* Use `env.tmp` (Debug only)

### Security

* Never commit secrets
* Use strong JWT keys (≥ 32 chars)

---

## 13. Performance Considerations

* Prefer async APIs
* Avoid blocking calls (`.Result`, `.Wait()`)
* Minimize allocations in hot paths
* Use batching where applicable

---

## 14. Coding Conventions

### Naming

* Activities: `VerbNoun` (e.g., `GenerateJwt`)
* Metrics: snake_case with domain prefix and suffix (e.g., umlaut_converter_requests_total)
* Scopes: meaningful business names

### Consistency

* Always include logging + tracing for key operations
* Reuse existing services (do not duplicate logic)

---

## 15. Extension Guidelines

When adding new features:

* Always ensure tracing is preserved across async boundaries
* If introducing messaging, ensure RabbitMQ context propagation is used
* Verify logs + metrics + traces are added consistently

---

## 16. Do’s and Don’ts

### Do

* Keep code modular
* Use DI properly
* Add observability everywhere

### Don’t

* Log sensitive data
* Create excessive metrics
* Block async flows

---
## 17. Distributed Systems Observability Rule

For any workflow involving async processing (queues, background services, SSE):

### Mandatory requirements:

* Create `Activity` at entry point (API layer)
* Ensure context propagation across async boundaries
* Maintain correlation using OpenTelemetry
* Validate trace continuity in Grafana Tempo

---

# 18. API Response Standardization

Add this after Error Handling.

---

## 18.1 Standard Response Contract

All non-SSE endpoints must return a standardized response:

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? TraceId { get; set; }
}
```

---

## 18.2 Behavior

| Scenario  | Behavior                        |
| --------- | ------------------------------- |
| Success   | Wrapped via `ApiResponseFilter` |
| Exception | Handled via middleware          |
| TraceId   | Comes from `Activity.TraceId`   |

---

## 18.3 Implementation Rules

* ❌ Do NOT manually wrap responses in controllers
* ✔ Use `ApiResponseFilter` for automatic wrapping
* ✔ Use middleware for exception handling

---

## 18.4 TraceId Rule (IMPORTANT)

```csharp
TraceId = Activity.Current?.TraceId.ToString()
```

> `HttpContext.TraceIdentifier` must NOT be used for distributed tracing.

---

---

# 19. SSE Response & Failure Handling

This is where your recent work really shines.

---

## 19.1 SSE Response Contract

SSE endpoints use:

```csharp
SseItem<ApiResponse<T>>
```

---

## 19.2 Event Types

| Event Type | Meaning               |
| ---------- | --------------------- |
| `query`    | Successful data event |
| `error`    | Failure event         |

---

## 19.3 Failure Handling Pattern (CRITICAL)

Exceptions must be handled **inside the stream**, not by middleware.

```csharp
try
{
    yield return successEvent;
}
catch (Exception ex)
{
    yield return errorEvent;
    yield break;
}
```

---

## 19.4 Why Middleware is NOT used for SSE

* SSE streams are long-lived
* Response starts early
* Middleware cannot modify response once streaming begins

👉 Therefore:

> SSE endpoints must handle errors explicitly and emit error events.

---

## 19.5 Chaos / Failure Simulation

Failures are triggered via **input-driven logic**, not test flags.

Example:

```csharp
if (input.Contains("error"))
{
    throw new QueryGenerationException(...);
}
```

---

## 19.6 Expected Client Behavior

Clients must:

* Handle `event: error`
* Stop consuming stream after error
* Use `TraceId` for debugging

---

## 19.7 Encoding Requirement (IMPORTANT)

SSE responses must use UTF-8:

```csharp
Response.Headers.ContentType = "text/event-stream; charset=utf-8";
```

---


### Target architecture principle:

> Every request must be traceable from entry to final output, even across message queues.

## Summary

This guide ensures that all contributions to the system are:

* Observable
* Consistent
* Secure
* Scalable

Follow these practices to maintain production-grade quality.
