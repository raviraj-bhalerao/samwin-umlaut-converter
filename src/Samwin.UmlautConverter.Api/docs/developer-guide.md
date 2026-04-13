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

## 3.3 Tracing

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

### 2.4 Metrics

Use `MetricsService` for all metrics.

**Examples:**

```csharp
_metricsService.RequestCounter.Add(1);
_metricsService.LoginAttempts.Add(1, new TagList { { "result", "success" } });
_metricsService.JwtGenerationTime.Record(duration);
```

**Guidelines:**

* Use **Counters** for counts
* Use **Histogram** for durations
* Always add tags for meaningful segmentation

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
    _metricsService.RequestCounter.Add(1, new TagList { { "endpoint", "example" } });

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
* Metrics: snake_case (e.g., `no_of_requests`)
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
## 17. Distributed Systems Observability Rule (NEW)

For any workflow involving async processing (queues, background services, SSE):

### Mandatory requirements:

* Create `Activity` at entry point (API layer)
* Ensure context propagation across async boundaries
* Maintain correlation using OpenTelemetry
* Validate trace continuity in Grafana Tempo

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
