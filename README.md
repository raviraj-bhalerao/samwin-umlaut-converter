# samwin-umlaut-converter

UmlautConverter library – modular, testable, and cloud/container-ready text conversion and variation generation, designed for the Samwin assignment and future AI integration.

---

## Features

- Converts common German umlaut sequences (`ae`, `oe`, `ue`, `ss`)
- Supports **both lowercase and uppercase inputs**
- Multiple implementations demonstrating different performance strategies
- Benchmarking using **BenchmarkDotNet**
- Unit tests validating correctness
- Modular architecture suitable for **cloud/container environments**

---

## Project Structure

The solution contains four projects:

```
samwin-umlaut-converter/
│
├── UmlautConverter
│   Core library containing the converter implementations.
│
├── UmlautConverter.Tests
│   Unit tests verifying correctness of all converters.
│
├── UmlautConverter.Benchmarks
│   BenchmarkDotNet benchmarks used to compare performance and memory usage.
│
└── UmlautConverter.Console
    Example console application demonstrating how to use the converters.
```

---

## Converter Implementations

Three converter implementations are provided, each demonstrating different performance trade-offs.

### UmlautSimpleConverter

Simple string-replacement based converter.

**Implementation**

- Uses multiple `string.Replace` operations.

**Complexity**

- `O(n × k)`
  - `n` = string length
  - `k` = number of mappings

**Advantages**

- Very easy to understand
- Minimal implementation complexity

**Disadvantages**

- Higher memory allocations
- Slower for large inputs

Best suited for **small inputs or simple applications**.

---

### UmlautEfficientConverter

Single-pass, buffer-based converter using `ReadOnlySpan<char>`.

**Implementation**

- Streaming approach processing characters once
- Uses a temporary buffer

**Complexity**

- `O(n)`

**Advantages**

- Fast execution
- Balanced memory usage

**Disadvantages**

- Slightly more complex implementation

Provides the **best balance between performance and memory usage**.

---

### UmlautMemoryOptimizedConverter

Single-pass converter that precomputes the final string size.

**Implementation**

- Uses `string.Create`
- Allocates the exact required output size
- Avoids intermediate allocations

**Complexity**

- `O(n)`

**Advantages**

- Lowest memory allocation
- Reduced GC pressure
- Suitable for high-throughput environments

**Disadvantages**

- Slightly slower due to preprocessing steps

Best suited for **memory-constrained systems or large-scale processing pipelines**.

---

## Example Usage

```csharp
using Samwin.UmlautConverterLib.Converters.Step1;

var converter = new UmlautEfficientConverter();

string input = "Muenchen ist schoen";
string result = converter.Convert(input);

Console.WriteLine(result);

// Output:
// München ist schön
```

The converters support **both lowercase and uppercase inputs**, for example:

```
MUENCHEN → MÜNCHEN
Muenchen → München
```

---

## Running the Project

Clone the repository:

```
git clone https://github.com/yourname/samwin-umlaut-converter.git
```

Build the solution:

```
dotnet build
```

Run the example console application:

```
dotnet run --project UmlautConverter.Console
```

---

## Running Unit Tests

Execute all tests using:

```
dotnet test
```

---

## Benchmark Results

Benchmarks were executed using **BenchmarkDotNet** on:

- OS: Windows 11
- CPU: Intel Celeron N5100
- Runtime: .NET 10
- Configuration: Release

### Performance Comparison (Execution Time)

| Repeat Count | SimpleConverter | EfficientConverter | MemoryOptimizedConverter |
|--------------|----------------|--------------------|--------------------------|
| 1,000 | 845 μs | **398 μs** | 2,413 μs |
| 10,000 | 9.56 ms | **3.27 ms** | 24.66 ms |
| 100,000 | 87.9 ms | **35.2 ms** | 263 ms |
| 500,000 | 454 ms | **184 ms** | 1.40 s |
| 1,000,000 | 1.07 s | **436 ms** | 2.76 s |
| 2,000,000 | 2.36 s | **1.02 s** | 4.61 s |

---

### Memory Allocation Comparison

| Repeat Count | SimpleConverter | EfficientConverter | MemoryOptimizedConverter |
|--------------|----------------|--------------------|--------------------------|
| 1,000 | 892 KB | 259 KB | **121 KB** |
| 10,000 | 8.9 MB | 2.6 MB | **1.2 MB** |
| 100,000 | 89 MB | 25.9 MB | **12.1 MB** |
| 500,000 | 446 MB | 129 MB | **60 MB** |
| 1,000,000 | 929 MB | 259 MB | **121 MB** |
| 2,000,000 | 1.8 GB | 519 MB | **242 MB** |

---

## Performance Summary

| Implementation | CPU Speed | Memory Usage |
|---------------|-----------|--------------|
| Simple | Medium | High |
| Efficient | **Fastest** | Moderate |
| MemoryOptimized | Slowest | **Lowest** |

For most real-world scenarios, **UmlautEfficientConverter provides the best balance between performance and memory usage**.

---

### When to Use Each Converter

| Converter | Best Use Case |
|-----------|---------------|
| Simple | Small inputs, quick prototyping, easy readability |
| Efficient | Medium to large inputs, balanced performance and memory |
| MemoryOptimized | High-throughput pipelines, memory-constrained environments, minimal GC pressure |

---

## Design Considerations

This project demonstrates several performance optimization strategies:

- simple readable implementation
- streaming single-pass conversion
- preallocated buffers to minimize allocations

These approaches highlight common engineering trade-offs between:

- **CPU performance**
- **memory usage**
- **code simplicity**

---

## Possible Future Improvements

- Support for additional German character transformations
- SIMD/vectorized text processing
- Streaming conversion for very large inputs
- Integration with AI-based text normalization pipelines