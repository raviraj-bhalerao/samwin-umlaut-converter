# Step 2 – Variation Generators

Step 2 implements multiple strategies to generate all possible variations of German umlaut sequences in text.  
The goal is to explore trade-offs between execution speed, memory usage, and implementation complexity for different input sizes.

Uppercase inputs are supported, and the generators differ in their approach to producing variations (recursive, iterative, buffered, streaming, etc.).

---

# Converter Implementations

This folder contains multiple implementations of the Step2 umlaut converter and their comparative analysis.  

**Key Features and Updates:**

- All generators implement the `IVariationGenerator` interface.
- **Input Validation:**  
  To prevent misuse on long texts, all generators now support optional validation (via a shared abstract base class):
  - Maximum string length (e.g., 50 characters)
  - Maximum number of words (e.g., 2–3 words)  
  Exceeding these limits throws descriptive exceptions.
- **Generator Strategies:**  
  1. **Bitmask-Based Generators:**  
     - `BitmaskEfficientVariationBufferGenerator`, `BitmaskEfficientVariationGenerator`, `BitmaskEfficientVariationYieldBufferGenerator`  
     - Iterative approach using bitmask to enumerate 2^k variations efficiently.  
     - Advantages: deterministic ordering, avoids recursion stack.
  2. **Branching Generators:**  
     - `BranchingVariationBufferGenerator`, `BranchingVariationGenerator`, `BranchingVariationYieldBufferGenerator`, `BranchingVariationYieldGenerator`  
     - Recursive tree approach, branches at each replaceable pair.  
     - Advantages: clear logic, all variations guaranteed.
  3. **Ground-Up / Simple Generators:**  
     - `SimpleGroundUpVariationGenerator`  
     - Easy-to-understand recursive implementation with result deduplication.
  4. **Streaming Generators:**  
     - `StreamingVariationGenerator`  
     - Generates variations lazily using `yield return`, minimizing memory usage.

- **Memory & Performance Considerations:**  
  - Some generators reuse buffers to minimize allocations (`Buffer` variants).  
  - Streaming generators avoid building full variation lists in memory.  
  - Exponential growth with number of replaceable pairs (O(n × 2^k)) is noted as a design constraint.

- **Defensive Programming:**  
  - Validation ensures generators are used only for realistic name inputs.  
  - Reduces risk of performance issues with long texts or malformed inputs.

This design allows selecting a generator based on **performance, memory usage, or readability requirements** while maintaining consistent handling of umlaut variations.

---

## SimpleGroundUpVariationGenerator

A straightforward recursive generator producing all variations from the ground up.

## Implementation

    - Recursively generates all permutations of replaceable characters.
    - Stores all results in memory.
    - Returns List<string> with all variations.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory allocations proportional to total variations.

## Advantages

- Easy to understand and maintain
- Produces all variations deterministically

## Disadvantages

- High memory usage
- Slow for larger inputs

## Best Use Case

- Small inputs such as names or short words

---

## BranchingVariationGenerator

Generates variations by branching at each replaceable character.

## Implementation

    - Iteratively scans input string.
    - At each replaceable character, creates branches in memory.
    - Returns List<string> with all permutations.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory proportional to number of variations.

## Advantages

- Avoids recursion
- Easier to debug than recursive methods

## Disadvantages

- Still allocates all results in memory
- Moderate performance on larger inputs

## Best Use Case

- Medium-sized strings where recursion may be costly

---

## BranchingVariationBufferGenerator

A memory-buffered version of the branching generator.

## Implementation

    - Similar to BranchingVariationGenerator.
    - Uses temporary buffers to minimize intermediate string allocations.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Reduced allocations compared to naive branching.

## Advantages

- Lower memory pressure
- Slightly faster for larger inputs

## Disadvantages

- Implementation slightly more complex

## Best Use Case

- Medium to moderately large strings

---

## BranchingVariationYieldGenerator

Streams variations using C# `yield return` instead of building full list in memory.

## Implementation

    - Generates variations lazily.
    - Each variation is returned one at a time via IEnumerable<string>.
    - No intermediate List<string> allocation.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
```O(1)``` memory per generated element.

## Advantages

- Very low memory footprint
- Suitable for streaming pipelines

## Disadvantages

- Slightly more CPU overhead per element
- Cannot index directly into results

## Best Use Case

- Large inputs where memory is constrained
- Scenarios consuming variations sequentially

---

## BranchingVariationYieldBufferGenerator

Hybrid approach: buffered branching with lazy yield.

## Implementation

    - Uses temporary buffers to reduce allocations.
    - Yields each variation lazily.
    - Combines benefits of memory buffering and streaming.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory per variation is minimized.

## Advantages

- Efficient memory usage
- Streams results
- Reduces GC pressure

## Disadvantages

- Implementation more complex
- Slightly slower than pure buffer approach for small inputs

## Best Use Case

- Large-scale variation generation in memory-sensitive environments

---

## BitmaskEfficientVariationGenerator

Generates variations using bitmask representations of replaceable characters.

## Implementation

    - Each replaceable character is represented by a bit.
    - Iterates over all possible bitmask combinations.
    - Produces List<string> of all variations.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory proportional to number of variations.

## Advantages

- Deterministic and fast for moderate input sizes
- Efficient iteration with bit operations

## Disadvantages

- Allocates full result list in memory
- Not suitable for very large n

## Best Use Case

- Medium strings where deterministic order is required

---

## BitmaskEfficientVariationBufferGenerator

Bitmask generator with buffer optimization.

## Implementation

    - Uses preallocated char buffers to reduce temporary string allocations.
    - Generates List<string> of variations.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory allocations optimized.

## Advantages

- Lower memory usage than naive bitmask generator
- Faster for repeated or larger inputs

## Disadvantages

- More complex implementation

## Best Use Case

- Medium to large strings with high variation counts

---

## BitmaskEfficientVariationYieldGenerator

Bitmask generator using lazy `yield return`.

## Implementation

    - Iterates over bitmask combinations.
    - Returns variations lazily via IEnumerable<string>.
    - No full list allocated.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory O(1) per element.

## Advantages

- Minimal memory allocation
- Suitable for streaming consumption

## Disadvantages

- Cannot directly index into results
- Slightly higher per-item overhead

## Best Use Case

- Very large inputs
- Memory-sensitive streaming pipelines

---

## BitmaskEfficientVariationYieldBufferGenerator

Hybrid bitmask generator with buffer and lazy yield.

## Implementation

    - Uses preallocated buffer for character replacements.
    - Generates variations lazily via `yield return`.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory per variation minimized.

## Advantages

- Low memory footprint
- Streams results
- Reduces GC pressure

## Disadvantages

- Most complex implementation
- Slight overhead for small inputs

## Best Use Case

- Large inputs where memory optimization and streaming are priorities

---

## StreamingVariationGenerator

Generates variations using a streaming pipeline approach.

## Implementation

    - Produces variations incrementally as data flows through the pipeline.
    - Designed for real-time or very large datasets.
    - Does not store all variations in memory.

## Complexity
```
O(2^n)
```
where `n` = number of replaceable characters.  
Memory usage constant per active element.

## Advantages

- Minimal memory footprint
- Suitable for large-scale streaming applications

## Disadvantages

- Slower than in-memory generators for small inputs
- Implementation more complex

## Best Use Case

- Very large inputs
- Real-time processing pipelines

---

# Benchmark Summary

## Key Insight

Benchmarks highlight that while all generators are theoretically O(2^n), **real-world performance varies due to memory allocations and iteration strategies**.

- Generators that allocate all results (SimpleGroundUp, Branching, Bitmask) are fast for small/medium inputs but memory-heavy.
- Generators using `yield` or streaming minimize memory but incur slight per-item CPU overhead.

## Recommended Usage by Scenario

| Scenario | Recommended Generator |
|----------|---------------------|
| Small inputs (e.g., names) | SimpleGroundUpVariationGenerator |
| Medium inputs, moderate memory | BranchingVariationBufferGenerator, BitmaskEfficientVariationBufferGenerator |
| Large inputs, memory-sensitive | BranchingVariationYieldGenerator, BitmaskEfficientVariationYieldGenerator |
| Large-scale streaming / pipelines | StreamingVariationGenerator, BranchingVariationYieldBufferGenerator, BitmaskEfficientVariationYieldBufferGenerator |

---

# Example Usage

```csharp
using Samwin.UmlautConverterLib.Step2;

var generator = new SimpleGroundUpVariationGenerator();

foreach(var variation in generator.Generate("Muenchen"))
{
    Console.WriteLine(variation);
}
```