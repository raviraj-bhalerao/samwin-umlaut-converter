using System;
using System.Buffers; // Essential for ArrayPool
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step1
{

    /// <summary>
    /// A memory-efficient converter that rents temporary buffers from
    /// <see cref="ArrayPool{T}"/> instead of allocating new arrays.
    /// </summary>
    /// <remarks>
    /// The algorithm performs a single pass over the input and uses a pooled
    /// buffer to construct the output string.
    ///
    /// Complexity:
    /// O(n)
    ///
    /// Benchmark observations:
    /// - Significantly reduces memory allocations compared to the Replace approach.
    /// - Slightly slower than Replace for small inputs.
    /// - Performs well when processing many large strings.
    ///
    /// Advantages:
    /// - Reduces GC pressure by reusing buffers
    /// - Scales better for high-volume workloads
    ///
    /// Disadvantages:
    /// - Slightly more complex code
    /// - ArrayPool management overhead
    ///
    /// Recommended usage:
    /// Ideal for high-throughput scenarios where many large strings
    /// must be processed and memory allocations should be minimized.
    /// </remarks>
    public class UmlautRentedHeapConverter : IUmlautConverter
    {
        private readonly UmlautMappingHelper _helper = new();
        public string Convert(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            ReadOnlySpan<char> src = input.AsSpan();

            // 1. Choose our buffer strategy
            char[]? rentedArray = null;

            // Stay under 256 chars (512 bytes) for stack safety
            Span<char> buffer = (rentedArray = ArrayPool<char>.Shared.Rent(src.Length));

            try
            {
                int outPos = 0;
                for (int i = 0; i < src.Length; i++)
                {
                    // Look ahead for the pair
                    if (i < src.Length - 1 && _helper.TryGetReplacement(src[i], src[i + 1], out var replacement))
                    {
                        buffer[outPos++] = replacement;
                        i++; // Consume the extra character (E or S)
                    }
                    else
                    {
                        buffer[outPos++] = src[i];
                    }
                }

                // 2. Return the result. 
                // Note: buffer.Slice(0, outPos) ensures we don't include trailing empty space 
                // from the rented array (since ArrayPool usually gives you a slightly larger array).
                return new string(buffer[0..outPos]);
            }
            finally
            {
                // 3. Crucial: Always return the memory to the pool!
                if (rentedArray != null)
                {
                    ArrayPool<char>.Shared.Return(rentedArray);
                }
            }
        }
    }
}