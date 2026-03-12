# samwin-umlaut-converter

[![Build & Test](https://github.com/raviraj-bhalerao/samwin-umlaut-converter/actions/workflows/Samwin-UmlautConverter_Build_Test.yml/badge.svg?branch=core)](https://github.com/raviraj-bhalerao/samwin-umlaut-converter/actions/workflows/Samwin-UmlautConverter_Build_Test.yml?query=branch%3Acore)
![Coverage](https://raw.githubusercontent.com/raviraj-bhalerao/samwin-umlaut-converter/core/badges/coverage.svg)


UmlautConverter is a modular .NET library for converting German umlaut sequences and generating text variations.  
The project was designed for the **Samwin technical assignment**, with a focus on clean architecture, performance benchmarking, and container-ready deployment.

---

# Features

- Converts common umlaut sequences (`ae`, `oe`, `ue`, `ss`) in upper, lower and mixed cases
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
│   ├── Samwin.UmlautConverterLib
│   │   Core library containing the converter implementations.
│   │
│   ├── Samwin.UmlautConverterLib.Tests
│   │   Unit tests verifying correctness of all converters.
│   │
│   ├── Samwin.UmlautConverter.ConsoleApp.Client
│   │   Example console application demonstrating how to use the converters.
│   │
│   ├── Samwin.UmlautConverter.Benchmarks
│   │   BenchmarkDotNet benchmarks used to compare performance and memory usage.
│   │
│   └── dockerfile
│       To build the containerised solution
│    
├── Samwin.UmlautConverter.sln
│   Solution for all projects
└── README.md
    Project overview and usage instructions.

````

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

```powershell
git clone https://github.com/yourname/samwin-umlaut-converter.git
````

Build the solution (run from **repo root**):

```powershell
dotnet build src/Samwin.UmlautConverter.sln
```

---

# Running Unit Tests

Run all tests (from **repo root**):

```powershell
dotnet test src/Samwin.UmlautConverterLib.Tests
```

---

# Local Coverage Setup

1. Restore local .NET tools (standardized via `dotnet-tools.json`) (from **repo root**):

```powershell
dotnet tool restore
```

2. Run tests and generate coverage report via provided PowerShell script (from **repo root**):

```powershell
.\scripts\coverage.ps1
```

This will:

* Run all unit tests with code coverage
* Generate an HTML report in `coverage-report/`
* Open `index.html` to view file-level and line-level coverage

# Viewing Code Coverage in GitHub Actions

The code coverage summary is available directly in the **GitHub Actions logs** for the workflow `Samwin UmlautConverter Build, Test & Coverage`.

### Steps to view coverage:

1. Navigate to the **Actions** tab in your repository.
2. Select the run you want to inspect of the workflow `Samwin UmlautConverter Build, Test & Coverage`.
3. Expand the log for task `Display coverage summary in log`
4. Look for the collapsible section `Code Coverage Summary`
5. Click the triangle to expand — this will show a **text summary** of coverage for projects, including:

* Total lines covered / total lines
* Coverage percentage
* Uncovered files or classes

> Tip: You can also download the full **HTML coverage report** artifact (uploaded by the workflow) for a detailed per-file and per-line view.

### Notes:

* `dotnet-tools.json` ensures all developers use the **same version of reportgenerator**
* Coverage report shows per-file coverage, uncovered lines, and overall statistics
* No global tools are required on contributors’ machines

---

# Benchmarking

Performance benchmarks are implemented using **BenchmarkDotNet**.

To run benchmarks locally (from **repo root**):

```powershell
dotnet run -c Release --project src/Samwin.UmlautConverter.Benchmarks
```

Benchmarks compare:

* execution time
* memory allocation
* scalability across input sizes

Detailed benchmark results are documented in the **Step1 documentation**.

---

# Continuous Integration / Continuous Delivery (CI/CD)

This repository uses **GitHub Actions** for automated build, testing, coverage, and benchmarking.

Workflow files are located in:

```
.github/workflows/
```

## Build, Test & Coverage Pipeline

The CI pipeline automatically runs on every push to the `core` branch.

Workflow file:

```
.github/workflows/Samwin-UmlautConverter_Build_Test.yml
```

The pipeline performs the following steps:

1. Checkout repository
2. Setup .NET SDK
3. Restore NuGet dependencies
4. Build the solution
5. Run all unit tests with code coverage

### Coverage Report in CI

* Code coverage is collected using `dotnet test --collect:"XPlat Code Coverage"`
* Coverage results are converted to HTML via **reportgenerator**
* Coverage summary (overall %) is displayed in GitHub Actions and can also be used for **README badges**
* The workflow publishes the HTML report as an artifact for inspection

This ensures that every commit is verified, tested, and coverage is tracked.

---

## Benchmark Workflow

Benchmarks can be executed **locally** or via a **manual GitHub Actions workflow**.

### Local Execution

From the **repo root**, run:

```powershell
dotnet run -c Release -p src/Samwin.UmlautConverter.Benchmarks/Samwin.UmlautConverter.Benchmarks.csproj
````

This will:

* Build the benchmark project
* Execute all BenchmarkDotNet benchmarks
* Output detailed performance metrics and memory usage to the console and `BenchmarkDotNet.Artifacts` folder

### GitHub Actions

Workflow file:

```
.github/workflows/Samwin-UmlautConverter_Benchmark.yml
```

Benchmarks are **not executed automatically** because they are time-consuming.

To run benchmarks in CI:

1. Open the **Actions** tab in GitHub.
2. Select **Samwin UmlautConverter Benchmarks**.
3. Click **Run workflow**.
4. Choose the `core` branch.

Benchmark results will appear in the workflow logs.

---

### CI/CD Summary

| Task          | Trigger           |
| ------------- | ----------------- |
| Build         | Automatic on push |
| Unit Tests    | Automatic on push |
| Code Coverage | Automatic on push |
| Benchmarks    | Manual workflow   |

---

# Logs / Test Output Inspection

When running the unit tests in GitHub Actions, you can inspect the generated output, including SQL statements or other debug information, using **collapsible groups**.

### How to view in GitHub Actions

1. Navigate to the **Actions** tab in your repository.
2. Select the run you want to inspect of the workflow `Samwin UmlautConverter Build, Test & Coverage`.
3. Expand the **job log** for task `Run tests with coverage`.
4. Look for the collapsible sections starting with your `Step# Output to inspect:`.

* Click the triangle to expand and see the full output.
* This keeps logs organized while showing detailed results such as all generated SQL statements or converted values.

---

# Design Goals

This project demonstrates several engineering concepts:

* multiple algorithm implementations for comparison
* performance benchmarking
* memory allocation optimization
* modular and testable architecture
* container-friendly design

---

# Dependency Injection Support

All library classes are designed to be **stateless and DI-friendly**.
For demonstration and simplicity, the console app and example usage do **not** use DI.

* Since the classes have no internal state, they can be safely registered as singleton or transient services.
* You can easily integrate them into your own DI container in production code:

```csharp
services.AddSingleton<IUmlautConverter, UmlautArrayConverter>();
services.AddSingleton<IVariationGenerator, BranchingVariationBufferGenerator>();
services.AddSingleton<ISqlQueryGenerator<SqlQuery>, ParameterizedSqlGenerator>();
```

---
# Static Code Analysis

This project integrates **static code analysis** using:

* `Microsoft.CodeAnalysis.NetAnalyzers`
* `Roslynator.Analyzers`
* `SonarAnalyzer.CSharp` (optional, for maintainability and complexity warnings)

Analyzers help enforce **code quality, style, and maintainability rules**.

---

### Running Analysis Locally

From the **repository root**, run:

```powershell
# Build the library project with analyzers
dotnet build src/Samwin.UmlautConverterLib/Samwin.UmlautConverterLib.csproj --no-restore --configuration Release
```

* This will output all **warnings and suggestions** generated by the analyzers.
* Warnings include things like **unused variables, long methods, high cyclomatic complexity, or naming/style issues**.
* To see warnings directly in **VS Code**, make sure the **C# extension (OmniSharp)** is installed — squiggly lines will appear for each warning.

> Tip: You can treat warnings as errors locally by adding the following to your `.csproj`:

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

---

### Running Analysis in GitHub Actions

The CI workflow automatically runs **static analysis** on every push to the `core` branch.

#### How to view in GitHub Actions
  1. Go to the **Actions** tab in the repository.
  2. Open the workflow run **“Samwin UmlautConverter Build, Test & Coverage”**.
  3. Expand the **“Code Analysis Warnings”** group.

> All analyzer warnings will appear here, similar to the coverage summary.

---

### Notes

* The output may **not include numeric complexity scores** like VS2022 Code Analysis, but warnings for **methods that are too long or too complex** will appear if the analyzers detect them.
* Using `.editorconfig`, you can **customize rules and severity levels** for the analyzers.

---

# Container Support

The project includes a Dockerfile allowing containerized execution.

Build container (from **repo root**):

```powershell
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

# Future Integration & Extensibility

This solution is designed not just for standalone execution but to integrate seamlessly into larger systems, including:

- **Web & UI Integration**: All converters and generators are exposed via clean interfaces and can be wrapped in REST APIs or used directly in TypeScript/Angular frontend pipelines.
- **Reactive / Actor-based Systems**: The modular architecture allows embedding into reactive pipelines, e.g., using Akka.NET actors, to process high-throughput text or query streams.
- **AI-enhanced Extensions**: Optional hooks exist to integrate AI-assisted normalization, variant resolution, or intelligent query suggestion, without affecting existing deterministic functionality.
- **Team & Collaboration Ready**: Full documentation, modular design, DI-ready architecture, and CI/CD pipelines enable easy adoption and extension by other developers.

# Role-Relevant Highlights

- **End-to-end engineering**: Implements conversion → variation generation → SQL query pipeline.
- **Performance & memory conscious**: Benchmarks, streaming vs buffered variants, ArrayPool, stackalloc, string.Create.
- **Quality & DevOps practices**: Unit tests, coverage, static analysis, CI/CD pipelines, containerization.
- **Modular & DI-ready**: All classes are stateless, easily injectable into other systems, and ready for cloud or microservices integration.
- **Extensible for future features**: AI-assisted or reactive system integration can be added without modifying core logic.
