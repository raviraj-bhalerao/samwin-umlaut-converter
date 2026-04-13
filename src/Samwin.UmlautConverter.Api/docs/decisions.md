## ADR-001: Why RabbitMQ?

### Context

The system needs to process umlaut conversion and SQL generation asynchronously without blocking API requests.

### Decision

We use RabbitMQ as a message broker to decouple request handling from processing.

### Consequences

**Positive:**

* Enables async processing
* Supports horizontal scaling of consumers
* Provides retry and buffering capabilities

**Trade-offs:**

* Adds infrastructure dependency
* Introduces eventual consistency

---

## ADR-002: Why SSE instead of WebSockets?

### Context

The system needs real-time updates from background processing to clients.

### Decision

We use Server-Sent Events (SSE) instead of WebSockets.

### Consequences

**Positive:**

* Simpler HTTP-based streaming model
* Easier infrastructure and debugging
* Ideal for one-way server → client updates

**Trade-offs:**

* Not suitable for bidirectional communication
* Limited flexibility compared to WebSockets

---

## ADR-003: Why collocated consumer?

### Context

Hosting constraints prevent deploying separate worker services.

### Decision

RabbitMQ consumer runs inside the same API process.

### Consequences

**Positive:**

* Simplified deployment
* Direct in-process event handling for SSE
* Lower operational overhead

**Trade-offs:**

* Reduced isolation between API and processing
* Scaling API and worker together

---

## ADR-004: Why ActivityService wrapper?

### Context

Direct usage of `ActivitySource` spreads tracing logic across the codebase.

### Decision

Introduce `IActivityService` abstraction over `ActivitySource`.

### Consequences

**Positive:**

* Centralized tracing logic
* Cleaner business code
* Easier testing/mocking

**Trade-offs:**

* Slight abstraction overhead

---

## ADR-005: Distributed Tracing Across RabbitMQ Using OpenTelemetry

### Context

In event-driven systems, trace context is lost when messages pass through a broker like RabbitMQ, breaking end-to-end observability.

### Decision

We implement OpenTelemetry context propagation across RabbitMQ using:

* `TextMapPropagator`
* Injection of `Activity.Current.Context` into message headers
* Extraction of context on consumer side
* Linking consumer activity as child span of producer

### Consequences

**Positive:**

* Full end-to-end distributed tracing (API → Queue → Consumer → SSE)
* Improved debugging across async workflows
* Accurate latency breakdown per processing stage
* Native integration with Grafana Tempo

**Trade-offs:**

* Increased complexity in messaging layer
* Requires strict discipline to avoid bypassing propagation logic
* Slight overhead in message header processing

---
