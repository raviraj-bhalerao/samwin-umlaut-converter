using System;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step1
{

    /// <summary>
    /// A high-performance converter that stores the output buffer on the stack
    /// using <c>stackalloc</c>.
    /// </summary>
    /// <remarks>
    /// This implementation avoids heap allocations for the temporary buffer
    /// by allocating memory directly on the stack.
    ///
    /// Complexity:
    /// O(n)
    ///
    /// Benchmark observations:
    /// - Reduces heap allocations.
    /// - Slower than expected for small inputs due to stack allocation overhead.
    /// - Unsafe for large inputs because the stack size is limited.
    ///
    /// Advantages:
    /// - No temporary heap allocation
    /// - Minimal GC pressure
    ///
    /// Disadvantages:
    /// - Risk of stack overflow for large inputs
    /// - Often slower than heap-based alternatives in practice
    ///
    /// Recommended usage:
    /// Suitable only for very small and predictable input sizes.
    /// Not recommended for general-purpose use.
    /// </remarks>
    public class UmlautStackAllocConverter : IUmlautConverter
    {
        private readonly UmlautMappingHelper _helper = new();
        public string Convert(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            ReadOnlySpan<char> span = input.AsSpan();
            // char[] buffer = new char[input.Length];
            Span<char> buffer = stackalloc char[input.Length];
            int pos = 0;

            for (int i = 0; i < span.Length; i++)
            {
                if (i < span.Length - 1 &&
                    _helper.TryGetReplacement(span[i], span[i + 1], out var rep))
                {
                    buffer[pos++] = rep;
                    i++;
                    continue;
                }

                buffer[pos++] = span[i];
            }

            // return new string(buffer, 0, pos);
            return new string(buffer.Slice(0, pos));

        }
    }
}