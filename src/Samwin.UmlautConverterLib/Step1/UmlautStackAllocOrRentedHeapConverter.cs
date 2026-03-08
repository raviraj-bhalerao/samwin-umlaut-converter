using System;
using System.Buffers; // Essential for ArrayPool
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step1
{

    /// <summary>
    /// A hybrid converter that uses stack allocation for small inputs
    /// and ArrayPool for larger inputs.
    /// </summary>
    /// <remarks>
    /// This implementation dynamically selects the most appropriate
    /// buffer strategy based on input size.
    ///
    /// Complexity:
    /// O(n)
    ///
    /// Benchmark observations:
    /// - Minimizes allocations similarly to the rented heap implementation.
    /// - Additional branching and logic introduces some overhead.
    /// - Did not outperform simpler implementations in most benchmarks.
    ///
    /// Advantages:
    /// - Flexible memory strategy
    /// - Avoids stack overflow risks for large inputs
    ///
    /// Disadvantages:
    /// - More complex implementation
    /// - Slightly slower than simpler approaches
    ///
    /// Recommended usage:
    /// Useful when both very small and very large inputs are expected
    /// and allocation patterns need to be balanced.
    /// </remarks>
    public class UmlautStackAllocOrRentedHeapConverter : IUmlautConverter
    {
        private readonly UmlautMappingHelper _helper = new();
        public string Convert(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            ReadOnlySpan<char> src = input.AsSpan();

            // 1. Choose our buffer strategy
            char[]? rentedArray = null;

            // Stay under 256 chars (512 bytes) for stack safety
            Span<char> buffer = src.Length <= 256
                ? stackalloc char[src.Length]
                : (rentedArray = ArrayPool<char>.Shared.Rent(src.Length));

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