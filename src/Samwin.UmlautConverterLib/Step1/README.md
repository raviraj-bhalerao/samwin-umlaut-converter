# Step 1 – Umlaut Converters

Step 1 implements several umlaut conversion strategies.

The goal is to explore different algorithmic approaches and analyze the trade-offs between:

- CPU performance
- memory allocation
- implementation complexity

All implementations convert common German character sequences.

| Sequence | Output |
|--------|--------|
| ae | ä |
| oe | ö |
| ue | ü |
| ss | ß |

Uppercase and mixed case inputs are also supported.

Example:

```
Muenchen → München
MUENCHEN → MÜNCHEN
```

---

# Converter Implementations

This folder contains multiple implementations of the Step2 umlaut converter and their comparative analysis.  

- Multiple converter strategies are provided:  
  `UmlautSimpleConverter`, `UmlautArrayConverter`, `UmlautRentedHeapConverter`, `UmlautStackAllocConverter`, `UmlautStackAllocOrRentedHeapConverter`, `UmlautStringCreateConverter`.
- Converters use different memory and allocation strategies:
  - **Simple string replacements** (`UmlautSimpleConverter`)  
  - **Array / Span-based processing** (`UmlautArrayConverter`, `UmlautStackAllocConverter`)  
  - **Memory-optimized creation** (`UmlautStringCreateConverter`)  
  - **Stack vs pooled heap selection** (`UmlautStackAllocOrRentedHeapConverter`)  
- The design emphasizes **high performance**, and **reusability**.
---

# UmlautSimpleConverter

A straightforward converter using repeated `string.Replace()` operations.

## Implementation

Each mapping is applied sequentially:

```
text.Replace("ae","ä")
    .Replace("oe","ö")
    .Replace("ue","ü");
```

## Complexity

```
O(n × k)
```

Where:

- `n` = input length  
- `k` = number of mappings

## Advantages

- Very simple implementation
- Easy to read and maintain

## Disadvantages

- Multiple passes over the string
- Higher memory allocations

## Best Use Case

Small inputs such as **personal names**.

---

# UmlautArrayConverter

A single-pass converter using a temporary character buffer.

## Implementation

- Processes input using `ReadOnlySpan<char>`
- Writes output into a `char[]` buffer

## Complexity

```
O(n)
```

## Advantages

- Single-pass processing
- Lower allocations than Replace

## Disadvantages

- Slightly more complex implementation
- Still allocates temporary buffers

## Best Use Case

General-purpose text processing.

---

# UmlautStackAllocConverter

A stack-based implementation using `stackalloc`.

## Implementation

Temporary buffers are allocated on the **stack** instead of the heap.

## Advantages

- No heap allocation for buffers
- Reduced GC pressure

## Disadvantages

- Limited stack size
- Risk of stack overflow for large inputs

## Best Use Case

Very small inputs where heap allocation should be avoided.

---

# UmlautRentedHeapConverter

Uses `ArrayPool<char>` to rent temporary buffers.

## Implementation

Buffers are rented from a shared memory pool and returned after use.

## Advantages

- Reduces allocations
- Scales well for large workloads

## Disadvantages

- Slightly more complex
- Small overhead for pool management

## Best Use Case

High-throughput workloads processing many strings.

---

# UmlautStackAllocOrRentedHeapConverter

A hybrid approach combining stack allocation and ArrayPool.

## Implementation

- small inputs → stack
- large inputs → ArrayPool

## Advantages

- flexible memory strategy

## Disadvantages

- slightly more complex implementation

## Best Use Case

mixed workloads with variable input sizes.

---

# UmlautStringCreateConverter

A memory-optimized converter using `string.Create()`.

## Implementation

Two-pass algorithm:

1. calculate final string length  
2. write directly into the string buffer

## Advantages

- only allocates the final string
- minimal memory usage

## Disadvantages

- two passes over input
- slightly slower

## Best Use Case

memory-sensitive environments.

---

# Example Usage

```csharp
using Samwin.UmlautConverterLib.Step1;

var converter = new UmlautArrayConverter();

string result = converter.Convert("Muenchen ist schoen");

Console.WriteLine(result);
```

Output:

```
München ist schön
```

---

# Benchmark Summary

## Key Insight

Although most converter implementations in this step have the same theoretical time complexity (`O(n)`), their real-world performance differs significantly.

This happens because **algorithmic complexity alone does not fully determine runtime behavior** for string-processing tasks of this size. Instead, performance is largely influenced by implementation details such as:

- number of passes over the input string
- temporary memory allocations
- garbage collection pressure
- runtime optimizations in the .NET string APIs

As a result, multiple algorithms with identical complexity can exhibit noticeably different CPU and memory characteristics.

This benchmark therefore focuses on **practical performance trade-offs between different implementation strategies**, rather than simply comparing theoretical complexity.

In practice, when algorithms share the same asymptotic complexity, **constant factors and memory behavior often dominate real-world performance**, especially for small to medium input sizes.

## Setup
Benchmarks were executed using BenchmarkDotNet on:

- OS: Windows 11
- Runtime: .NET 10
- CPU: Intel Celeron N5100
- Configuration: Release

The goal of the benchmarks was to compare the trade-offs between:

- execution speed
- memory allocations
- implementation complexity

Rather than focusing on a single “fastest” implementation, the results highlight how different strategies perform under different workloads.

---

## Implementation Comparison

| Converter | Strategy | CPU Performance | Memory Usage | Pros | Cons | Best Use Case |
|-----------|----------|----------------|--------------|------|------|---------------|
| **UmlautSimpleConverter** | Multiple `string.Replace()` calls | Fast for small inputs | High | Very simple implementation | Multiple passes over string, high allocations | Small strings (e.g. names) |
| **UmlautArrayConverter** | Single-pass with `char[]` buffer | Fast | Moderate | Predictable performance, simple single-pass algorithm | Requires temporary buffer allocation | General-purpose conversion |
| **UmlautStackAllocConverter** | Stack allocated buffer | Moderate | Low | Avoids heap allocations | Limited stack size, not suitable for large inputs | Very small inputs |
| **UmlautRentedHeapConverter** | `ArrayPool<char>` buffer reuse | Fast for repeated workloads | Low | Reduces GC pressure | Slight pool management overhead | High-throughput systems |
| **UmlautStackAllocOrRentedHeapConverter** | Hybrid stack / pooled buffer | Moderate | Low | Adapts to input size | More complex logic | Mixed workloads |
| **UmlautStringCreateConverter** | `string.Create()` with precomputed size | Slower | Lowest | Allocates only the final string | Requires two passes | Memory-sensitive environments |

---

## Key Observations

The benchmarks highlight several important engineering trade-offs:

### 1. Simplicity vs Performance

`UmlautSimpleConverter` is extremely simple and performs well for short strings, but its repeated `Replace` calls cause higher memory allocations.

### 2. Single-Pass Processing Improves Scalability

Converters such as `UmlautArrayConverter` process the input in a single pass, providing predictable performance as input sizes grow.

### 3. Memory Optimization Techniques

Techniques such as:

- `stackalloc`
- `ArrayPool<T>`
- `string.Create`

reduce memory allocations and GC pressure, which can be important in high-throughput systems.

### 4. No Single Implementation Is Universally Best

Different implementations perform best depending on the workload:

| Scenario | Recommended Converter |
|--------|-----------------------|
| Small inputs | `UmlautSimpleConverter` |
| Balanced performance | `UmlautArrayConverter` |
| High throughput workloads | `UmlautRentedHeapConverter` |
| Memory-constrained environments | `UmlautStringCreateConverter` |

---

## Engineering Takeaway

This step demonstrates how multiple implementations of the same algorithm can exhibit different performance characteristics depending on the chosen optimization strategy.

Benchmark-driven development helps identify the most appropriate implementation for specific real-world workloads.