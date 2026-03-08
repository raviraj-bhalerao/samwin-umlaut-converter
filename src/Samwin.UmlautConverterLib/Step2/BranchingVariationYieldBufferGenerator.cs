using System;
using System.Collections.Generic;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Combines buffer reuse with yield for branching generator.
    /// </summary>
    /// <remarks>
    /// Strategy: Pre-allocated buffer + streaming (yield) for large sequences.
    /// Complexity: O(2^n); memory-efficient for streaming large sequences.
    /// Benchmark observations: Slower than buffer-only version for small inputs; reduces memory allocations for large inputs.
    /// Advantages:
    /// - Streaming variations with minimal memory allocations
    /// - Suitable for large workloads
    /// Disadvantages:
    /// - Slower than non-yield buffer variant for small inputs
    /// Recommended usage:
    /// - Large text streams where full in-memory generation is impractical.
    /// </remarks>
    public class BranchingVariationYieldBufferGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();

        public IEnumerable<string> Generate(string input)
        {
            // Validate input first
            this.ValidateInput(input);

            if (string.IsNullOrEmpty(input))
            {
                yield return input;
                yield break;
            }

            // Use a simple char array as a scratchpad to avoid intermediate strings
            char[] buffer = new char[input.Length];

            foreach (var variation in GenerateRecursive(input, 0, buffer, 0))
                yield return variation;
        }

        private IEnumerable<string> GenerateRecursive(string input, int srcIdx, char[] buffer, int destIdx)
        {
            if (srcIdx >= input.Length)
            {
                // Only allocate the string at the very end of the branch
                yield return new string(buffer, 0, destIdx);
                yield break;
            }

            if (srcIdx < input.Length - 1 && _helper.TryGetReplacement(input[srcIdx], input[srcIdx + 1], out var replacement))
            {
                // Branch 1: Keep original (AE)
                buffer[destIdx] = input[srcIdx];
                buffer[destIdx + 1] = input[srcIdx + 1];
                foreach (var v in GenerateRecursive(input, srcIdx + 2, buffer, destIdx + 2))
                    yield return v;

                // Branch 2: Replace (Ä)
                buffer[destIdx] = replacement;
                foreach (var v in GenerateRecursive(input, srcIdx + 2, buffer, destIdx + 1))
                    yield return v;
            }
            else
            {
                // No replacement possible
                buffer[destIdx] = input[srcIdx];
                foreach (var v in GenerateRecursive(input, srcIdx + 1, buffer, destIdx + 1))
                    yield return v;
            }
        }
    }
}