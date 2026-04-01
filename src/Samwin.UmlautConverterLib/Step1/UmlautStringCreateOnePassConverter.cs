using System;
using System.Collections.Generic;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step1
{

    /// <summary>
    /// A memory-optimized converter that creates the final string directly
    /// using <see cref="string.Create(int, TState, SpanAction{char,TState})"/>.
    /// </summary>
    /// <remarks>
    /// This implementation performs two passes:
    /// 1. Calculate the exact output length.
    /// 2. Write the result directly into the allocated string buffer.
    ///
    /// Complexity:
    /// O(n)
    ///
    /// Benchmark observations:
    /// - Allocates only the final string.
    /// - Lowest memory usage among all implementations.
    /// - Slower than single-pass approaches due to the additional pass.
    ///
    /// Advantages:
    /// - Minimal memory allocations
    /// - Predictable memory usage
    ///
    /// Disadvantages:
    /// - Two passes over the input
    /// - Higher CPU cost
    ///
    /// Recommended usage:
    /// Best suited for scenarios where minimizing allocations and
    /// GC pressure is more important than raw execution speed.
    /// </remarks>
    public class UmlautStringCreateOnePassConverter : IUmlautConverter
    {
        private readonly UmlautMappingHelper _helper = new();
        // private static readonly Dictionary<(char, char), char> _umlautMappings = new()
        // {
        //     // Lowercase
        //     { ('a', 'e'), 'ä' },
        //     { ('o', 'e'), 'ö' },
        //     { ('u', 'e'), 'ü' },
        //     { ('s', 's'), 'ß' },

        //     // Mixed-case first letter
        //     { ('A', 'e'), 'Ä' },
        //     { ('O', 'e'), 'Ö' },
        //     { ('U', 'e'), 'Ü' },

        //     // Fully uppercase
        //     { ('A', 'E'), 'Ä' },
        //     { ('O', 'E'), 'Ö' },
        //     { ('U', 'E'), 'Ü' },
        //     { ('S', 'S'), 'ẞ' } // optional
        // };

        public string Convert(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            ReadOnlySpan<char> span = input.AsSpan();

            // First pass: calculate exact output length
            int finalLength = input.Length;

            // for (int i = 0; i < span.Length; i++)
            // {
            //     if (i < span.Length - 1 && _helper.TryGetReplacement(span[i], span[i + 1], out _))
            //     {
            //         finalLength++;
            //         i++; // skip second char of the pair
            //     }
            //     else
            //     {
            //         finalLength++;
            //     }
            // }

            int outPos = 0;
            // Allocate the final string with the exact required size
            string toRet = string.Create(finalLength, span, (output, src) =>
            {

                for (int i = 0; i < src.Length; i++)
                {
                    if (i < src.Length - 1 &&
                        _helper.TryGetReplacement(src[i], src[i + 1], out var replacement))
                    {
                        output[outPos++] = replacement;
                        i++; // skip next char
                    }
                    else
                    {
                        output[outPos++] = src[i];
                    }
                }

                // // Safety check: ensures algorithm consistency
                // if (outPos != output.Length)
                //     throw new InvalidOperationException("Output length mismatch.");
            });
            return toRet.Substring(0, outPos);
        }
    }
}