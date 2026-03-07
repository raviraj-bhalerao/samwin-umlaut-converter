using System;
using System.Collections.Generic;

namespace Samwin.UmlautConverterLib.Step1;

/// <summary>
/// A memory-optimized converter that minimizes intermediate allocations.
/// </summary>
/// <remarks>
/// This implementation performs two passes:
/// 1. First pass calculates the exact output length.
/// 2. Second pass writes the output using <see cref="string.Create"/>.
///
/// Complexity:
/// O(n)
/// where n = length of the input string.
///
/// Advantages:
/// - Allocates exactly one output string
/// - Minimizes memory usage
/// - Mapping logic is configurable via dictionary
///
/// Disadvantages:
/// - Two passes over the input string
/// - Slower than the efficient converter in CPU time
///
/// Best suited for scenarios where memory usage or GC pressure
/// is more important than raw execution speed.
///
/// Note:
/// This implementation assumes fixed-length patterns (2 characters).
/// Variable-length patterns would require additional logic.
/// </remarks>
public class UmlautMemoryOptimizedConverter
{
private static readonly Dictionary<(char, char), char> _umlautMappings = new()
{
    // Lowercase
    { ('a', 'e'), 'ä' },
    { ('o', 'e'), 'ö' },
    { ('u', 'e'), 'ü' },
    { ('s', 's'), 'ß' },

    // Mixed-case first letter
    { ('A', 'e'), 'Ä' },
    { ('O', 'e'), 'Ö' },
    { ('U', 'e'), 'Ü' },

    // Fully uppercase
    { ('A', 'E'), 'Ä' },
    { ('O', 'E'), 'Ö' },
    { ('U', 'E'), 'Ü' },
    { ('S', 'S'), 'ẞ' } // optional
};

    public string Convert(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        ReadOnlySpan<char> span = input.AsSpan();

        // First pass: calculate exact output length
        int finalLength = 0;

        for (int i = 0; i < span.Length; i++)
        {
            if (i < span.Length - 1 && _umlautMappings.ContainsKey((span[i], span[i + 1])))
            {
                finalLength++;
                i++; // skip second char of the pair
            }
            else
            {
                finalLength++;
            }
        }

        // Allocate the final string with the exact required size
        return string.Create(finalLength, span, (output, src) =>
        {
            int outPos = 0;

            for (int i = 0; i < src.Length; i++)
            {
                if (i < src.Length - 1 &&
                    _umlautMappings.TryGetValue((src[i], src[i + 1]), out var replacement))
                {
                    output[outPos++] = replacement;
                    i++; // skip next char
                }
                else
                {
                    output[outPos++] = src[i];
                }
            }

            // Safety check: ensures algorithm consistency
            if (outPos != output.Length)
                throw new InvalidOperationException("Output length mismatch.");
        });
    }
}