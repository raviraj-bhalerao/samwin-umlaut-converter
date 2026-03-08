using System;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step1
{

    /// <summary>
    /// A single-pass converter that scans the input and writes the result
    /// to a preallocated character array.
    /// </summary>
    /// <remarks>
    /// The converter processes the input using <see cref="ReadOnlySpan{T}"/>
    /// and writes results into a temporary char buffer before creating the
    /// final string.
    ///
    /// Complexity:
    /// O(n)
    ///
    /// Benchmark observations:
    /// - Slightly slower than the simple Replace-based implementation for small inputs.
    /// - Uses significantly less memory than the Replace approach.
    /// - Performance scales predictably with large input sizes.
    ///
    /// Advantages:
    /// - Single pass algorithm
    /// - Lower memory allocation than repeated Replace
    /// - Simple and predictable performance
    ///
    /// Disadvantages:
    /// - Slightly more complex code
    /// - Still allocates a buffer equal to the input length
    ///
    /// Recommended usage:
    /// Good general-purpose implementation when processing larger texts
    /// or when allocation pressure should be reduced.
    /// </remarks>
    public class UmlautArrayConverter : IUmlautConverter
    {
        private readonly UmlautMappingHelper _helper = new();
        public string Convert(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            ReadOnlySpan<char> span = input.AsSpan();
            char[] buffer = new char[input.Length];
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

            return new string(buffer, 0, pos);

        }
    }
}