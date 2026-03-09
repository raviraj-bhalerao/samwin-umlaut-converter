# samwin-umlaut-converter

UmlautConverter is a modular .NET library for converting German umlaut sequences and generating text variations.  
The project was designed for the **Samwin technical assignment**, with a focus on clean architecture, performance benchmarking, and container-ready deployment.

---

# Features

- Converts common umlaut sequences (`ae`, `oe`, `ue`, `ss`)
- Supports **both lowercase and uppercase inputs**
- Multiple converter implementations demonstrating different performance strategies
- Variation generators producing umlaut permutations
- SQL generation utilities for database search scenarios
- Benchmarking using BenchmarkDotNet
- Comprehensive unit testing
- Designed for cloud and container environments

---

# Project Structure

```
samwin-umlaut-converter/
│
├── src/
│   ├── UmlautConverter
│   │   Core library containing the converter implementations.
│   │
│   ├── UmlautConverter.Tests
│   │   Unit tests verifying correctness of all converters.
│   │
│   ├── UmlautConverter.Benchmarks
│   │   BenchmarkDotNet benchmarks used to compare performance and memory usage.
│   │
│   ├── UmlautConverter.Console
│   │   Example console application demonstrating how to use the converters.
│   │
├── README.md
│   Project overview and usage instructions.
│
```

---

# Documentation

Detailed documentation for each step is located inside the corresponding folders:

| Step | Description | Documentation |
|-----|-------------|--------------|
| Step 1 | Umlaut text converters and performance benchmarks | [Step1 Documentation](src/Samwin.UmlautConverterLib/Step1/README.md) |
| Step 2 | Variation generators for umlaut permutations | [Step2 Documentation](src/Samwin.UmlautConverterLib/Step2/README.md) |
| Step 3 | SQL query generation utilities | [Step3 Documentation](src/Samwin.UmlautConverterLib/Step3/README.md) |

---

# Running the Project

Clone the repository:

```
git clone https://github.com/yourname/samwin-umlaut-converter.git
```

Build the solution:

```
dotnet build
```

---

# Running Unit Tests

Run all tests using:

```
dotnet test
```

---

# Benchmarking

Performance benchmarks are implemented using BenchmarkDotNet.

To run benchmarks:

```
dotnet run -c Release --project Samwin.UmlautConverterLib.Benchmarks
```

Benchmarks compare:

- execution time
- memory allocation
- scalability across input sizes

Detailed benchmark results are documented in the **Step1 documentation**.

---

# Running Benchmarks

Benchmarks can be executed manually via GitHub Actions:

1. Go to the **Actions** tab in the repository.  
2. Select the **Samwin-UmlautConverter Benchmarks** workflow.  
3. Click **Run workflow** → select the `core` branch → **Run workflow**.  
4. The workflow will build the project and execute all BenchmarkDotNet benchmarks.  

Results are available in the workflow logs.

# Design Goals

This project demonstrates several engineering concepts:

- multiple algorithm implementations for comparison
- performance benchmarking
- memory allocation optimization
- modular and testable architecture
- container-friendly design

---
# Dependency Injection Support

All library classes are designed to be **stateless and DI-friendly**.  
For demonstration and simplicity, the console app and example usage do **not** use DI.  

- Since the classes have no internal state, they can be safely registered as singleton or transient services.  
- You can easily integrate them into your own DI container in production code:

```csharp
services.AddSingleton<IUmlautConverter, UmlautArrayConverter>();
services.AddSingleton<IVariationGenerator, BranchingVariationBufferGenerator>();
services.AddSingleton<ISqlQueryGenerator<SqlQuery>, ParameterizedSqlGenerator>();
```
---
# Container Support

The project includes a Dockerfile allowing containerized execution.

Build container:

```
docker build -t samwin-umlaut-converter .
```

---

# Advanced / Experimental Implementation

Planned ideas for a more advanced version of the library include:

### Caching strategies for improved performance
Introduce caching mechanisms to avoid recomputing variations for inputs that have already been processed. This could significantly improve performance in scenarios where the same names or identifiers appear repeatedly. Different cache implementations (in-memory or distributed) could be evaluated depending on the deployment environment.

### AI-based placeholder for name resolution and normalization
Explore the use of AI-assisted approaches to improve name normalization and variant resolution. This could help handle edge cases where simple character replacement is insufficient, such as cultural naming variations or ambiguous transliterations. The current architecture is designed so such functionality could be integrated as an optional extension.

### Configuration-based switching between plain and AI implementations
Allow users to switch between the deterministic implementation and an AI-assisted implementation through configuration. This ensures predictable behavior for default use cases while enabling experimentation with more advanced resolution strategies. The goal is to maintain backward compatibility while supporting extensibility.

### External umlaut mapping storage via file or database
Move the umlaut and character mapping definitions from hardcoded structures into configurable external storage. This would allow updates or customization without recompiling the library. Possible sources include JSON/YAML configuration files or database-backed mappings.

### Full Dependency Injection ready architecture
Design the library so all core components can be wired through dependency injection. This allows consumers to replace implementations (e.g., caching, formatting, or mapping providers) without modifying the core library. It also improves testability and integration with modern application frameworks.

### Optional rate limiting for high-throughput scenarios
Introduce optional rate limiting mechanisms to control how frequently expensive operations can be executed. This can be useful when AI-based or external service integrations are added later. It helps protect external dependencies and maintain predictable system performance under load.

### Extend unit tests for advanced features
Expand the test suite to cover advanced behaviors such as caching, configuration switching, and external mapping providers. Additional tests would ensure consistent behavior across different implementations. This would also help validate extensibility points and guard against regressions.

