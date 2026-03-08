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

# Design Goals

This project demonstrates several engineering concepts:

- multiple algorithm implementations for comparison
- performance benchmarking
- memory allocation optimization
- modular and testable architecture
- container-friendly design

---

# Container Support

The project includes a Dockerfile allowing containerized execution.

Build container:

```
docker build -t samwin-umlaut-converter .
```

---

# Future Improvements

Possible future enhancements include:

- additional language normalization rules
- SIMD/vectorized text processing
- streaming text processing pipelines
- AI-based text normalization integration

---

# License

MIT License