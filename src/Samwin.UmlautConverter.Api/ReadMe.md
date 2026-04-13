# Samwin Umlaut Converter API

## Overview

The **Samwin Umlaut Converter API** is a scalable, event-driven .NET API that converts input tokens (including umlauts) into multiple variations and generates SQL queries in a streaming fashion.

The system is designed with **asynchronous processing**, **real-time streaming (SSE)**, and **production-grade observability** using tracing, metrics, and structured logging.

The system is implemented using an **event-driven, observable, distributed .NET system with RabbitMQ, SSE, and OpenTelemetry instrumentation**.

---

## Architecture Overview

The application follows a **hybrid synchronous + asynchronous architecture**:

* **API Layer** – Handles requests, authentication, and streaming responses
* **Message Bus (RabbitMQ)** – Decouples request processing
* **Background Consumer** – Processes messages and generates results
* **Converter Library** – Generates variations and SQL queries

### Flow

1. Client sends request → API
2. API publishes message → RabbitMQ
3. Background service consumes message
4. Variations & SQL queries are generated
5. Results streamed back via **Server-Sent Events (SSE)**

---

## Features

* JWT Authentication (Password & Google login)
* Umlaut conversion and variation generation
* SQL query generation (parameterized)
* Server-Sent Events (real-time streaming)
* RabbitMQ integration for async processing
* OpenTelemetry (Tracing & Metrics)
* Structured logging with NLog
* Swagger with JWT support

---

## Tech Stack

* .NET 10 (ASP.NET Core Web API)
* RabbitMQ
* OpenTelemetry
* NLog
* Swagger (Swashbuckle)
* JWT Authentication
* Google Authentication

---

## Getting Started

### Prerequisites

* .NET SDK (latest)
* RabbitMQ (local or remote)
* (Optional) Grafana OTLP endpoint for observability

---

### Configuration

The application relies on environment variables.

For local development, use:

```
env.tmp
```

This file contains required environment variables and is loaded automatically in **Debug mode**.

#### Key Configurations

* `JwtSettings` (TokenKey, Issuer, Audience)
* `RABBIT_MQ_URI`
* `GRAFANA_OTLP_ENDPOINT`
* `GRAFANA_OTLP_INSTANCE_ID`
* `GRAFANA_OTLP_ACCESS_TOKEN`

---

### Run the Application

```bash
dotnet build
dotnet run
```

Swagger UI will be available at:

```
https://localhost:<port>/swagger
```

---

## Configuration Details

### JWT Settings

Configured via `JwtSettings` section:

* TokenKey (min 32 chars)
* TokenIssuer
* TokenAudience

---

### RabbitMQ

* Uses default queue: `my_demo_queue`
* URI configured via environment variable

---

### OpenTelemetry

* Tracing and metrics exported via OTLP
* Supports Grafana integration
* Console exporter enabled in Debug mode

---

### Logging

* Implemented using NLog
* Logs available via `/logs` endpoint

---

## API Endpoints

### Auth

#### POST `/auth/LoginWithPassword`

* Authenticates using username/password
* Returns JWT token

#### POST `/auth/GoogleLogin`

* Accepts Google ID token
* Validates via Google
* Returns JWT token

---

### Query Generator

#### GET `/QueryGenerator/GetQuery`

* Accepts multiple inputs
* Publishes to queue
* Streams generated SQL queries via SSE

---

### Weather (Demo/Test)

#### GET `/WeatherForecast`

* Returns sample weather data

#### GET `/WeatherForecast/secure`

* Requires JWT authentication

#### GET `/WeatherForecast/heart`

* Streams heartbeat events via SSE

#### GET `/WeatherForecast/QueueMsg`

* Demo endpoint for queue + SSE flow

---

## Authentication & Authorization

### JWT Authentication

* Tokens generated using symmetric key (HS256)
* Includes:

  * Subject
  * Email
  * Roles

---

### Google Authentication

* Accepts Google ID token
* Validated using Google API
* Issues internal JWT with roles

---

### Authorization

* Role-based authorization supported
* Example:

```
[Authorize(Roles = "Admin")]
```

---

## Data Flow

1. Client sends request with inputs
2. API publishes message to RabbitMQ
3. Background consumer processes message
4. Variations generated
5. SQL queries created
6. Results streamed back via SSE

---

## Observability

### Tracing

* Implemented using OpenTelemetry
* Activity-based tracing via `ActivityService`

---

### Metrics

* Custom metrics include:

  * Login attempts
  * Requests count
  * Tokens converted
  * Queries generated
  * Variations created
  * JWT generation time

---

### Logging

* Structured logging using scopes
* Centralized via NLog
* Supports correlation with tracing

---

## Message Queue (RabbitMQ)

* Queue: `my_demo_queue`
* Producer: API
* Consumer: Background service
* Messages acknowledged manually
* Failed messages are requeued

---

## Background Processing

* Implemented via hosted service
* Continuously consumes messages from queue
* Generates SQL queries asynchronously

---

## Project Structure

```
/Api
  /Controllers
  /Services
  /Models
  /Settings
/Library (UmlautConverterLib)
```

---

## Developer Guide

For extending the system (adding converters, observability, auth, etc.), see:

```
docs/developer-guide.md
```

---

## Development Notes

* `env.tmp` is used only in Debug mode
* SSE streams close after 30s inactivity
* Uses async patterns throughout
* Logging scopes include OperationId for tracing

---

## Future Improvements

* Dead-letter queue support
* Retry policies
* Rate limiting
* Caching layer

---

## 📚 Documentation

Detailed system documentation is available in the `/docs` folder:

### 🧭 Architecture & Design

* [Architecture Overview](docs/architecture.md) – Event-driven system design, RabbitMQ flow, SSE streaming, and system boundaries
* [Architecture Decisions (ADR)](docs/decisions.md) – Key architectural decisions and trade-offs

---

### 🔍 Observability

* [Observability Guide](docs/observability.md) – OpenTelemetry tracing, metrics, logging, and Grafana integration

---

### 👨‍💻 Developer Guidance

* [Developer Guide](docs/developer-guide.md) – Coding patterns, extending system components, and implementation rules
* [Code Review Checklist](docs/code-review-checklist.md) – PR review standards and quality gates

---
