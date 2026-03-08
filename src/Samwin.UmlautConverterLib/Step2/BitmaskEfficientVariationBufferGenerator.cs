using System.Collections.Generic;
using Samwin.UmlautConverterLib.Step1;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Optimized buffer-based bitmask generator minimizing allocations.
    /// </summary>
    /// <remarks>
    /// Strategy: Iterative bitmask generation with pre-allocated buffer.
    /// Complexity: O(2^n); minimal memory allocations.
    /// Benchmark observations: Faster and more memory-efficient than non-buffer bitmask; still slower than branching buffer for small inputs.
    /// Advantages:
    /// - Low memory allocations
    /// - Avoids recursion
    /// Disadvantages:
    /// - Slightly slower than branching buffer for short inputs
    /// Recommended usage:
    /// - Medium-length strings where deterministic, iterative generation is needed.
    /// </remarks>
    public class BitmaskEfficientVariationBufferGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();
        private readonly UmlautStackAllocConverter _converter = new();

        public IEnumerable<string> Generate(string input)
        {

            // Validate input first
            this.ValidateInput(input);

            if (string.IsNullOrWhiteSpace(input))
                return new List<string> { input };

            var positions = new List<(int index, char replacement)>();
            for (int i = 0; i < input.Length - 1; i++)
            {
                if (_helper.TryGetReplacement(input[i], input[i + 1], out var rep))
                {
                    positions.Add((i, rep));
                    i++; // skip next char
                }
            }

            int k = positions.Count;
            int variationsCount = 1 << k;
            var results = new List<string>(variationsCount);

            // One buffer for the whole method
            char[] buffer = new char[input.Length];

            for (int mask = 0; mask < variationsCount; mask++)
            {
                int srcIdx = 0;
                int destIdx = 0;
                int bitIdx = 0;

                while (srcIdx < input.Length)
                {
                    // If we are at a "Branch Point" and the bit is set
                    if (bitIdx < k && srcIdx == positions[bitIdx].index)
                    {
                        bool shouldReplace = (mask & (1 << bitIdx)) != 0;
                        if (shouldReplace)
                        {
                            buffer[destIdx++] = positions[bitIdx].replacement;
                            srcIdx += 2; // Skip the pair (AE -> Ä)
                        }
                        else
                        {
                            buffer[destIdx++] = input[srcIdx++];
                            buffer[destIdx++] = input[srcIdx++]; // Keep original (AE -> AE)
                        }
                        bitIdx++;
                    }
                    else
                    {
                        buffer[destIdx++] = input[srcIdx++];
                    }
                }
                results.Add(new string(buffer, 0, destIdx));
            }
            return results;
        }
    }
}
