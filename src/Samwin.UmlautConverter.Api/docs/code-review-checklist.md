## 1. Observability

* [ ] Structured logging used (`ILogger<T>`)
* [ ] Logging scopes include `Scope` and `OperationId`
* [ ] Tracing added for key operations via `IActivityService`
* [ ] Metrics updated where applicable
* [ ] No missing correlation between logs, metrics, and traces

---

## 2. Distributed Tracing & Messaging

* [ ] RabbitMQ messages use `PublishMessageAsync`
* [ ] Payloads are minimal and idempotent
* [ ] Consumer properly acknowledges/nacks messages
* [ ] Trace context is preserved across message boundaries
* [ ] No manual bypass of OpenTelemetry propagation logic

---

## 3. API Layer

* [ ] Proper endpoint naming and grouping
* [ ] Authentication applied where required
* [ ] Authorization attributes correct
* [ ] Cancellation tokens respected
* [ ] No blocking calls in async flows

---

## 4. Security

* [ ] No sensitive data in logs
* [ ] JWT claims are intentional and minimal
* [ ] Secrets are never hardcoded
* [ ] Role-based authorization used appropriately

---

## 5. Performance

* [ ] No `.Result` or `.Wait()`
* [ ] Async flow preserved end-to-end
* [ ] No unnecessary allocations in hot paths
* [ ] Metrics do not introduce high cardinality

---

## 6. Architecture Consistency

* [ ] Event-driven patterns followed
* [ ] No direct coupling between API and processing logic
* [ ] Converter logic reused via DI
* [ ] No duplication of domain logic

---

## 7. SSE / Streaming (if applicable)

* [ ] Handles client disconnect correctly
* [ ] Uses cancellation tokens properly
* [ ] Avoids blocking loops
* [ ] Streaming is incremental and non-batching

---

## 8. Final Check

* [ ] Feature is observable
* [ ] Feature is testable
* [ ] Feature is scalable
* [ ] Feature is consistent with existing patterns

---
