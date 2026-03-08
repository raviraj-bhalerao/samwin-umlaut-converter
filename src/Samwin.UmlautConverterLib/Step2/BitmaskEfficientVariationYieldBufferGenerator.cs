using System.Collections.Generic;
using Samwin.UmlautConverterLib.Step1;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
/// <summary>
/// Buffer + yield variant of bitmask generator.
/// </summary>
/// <remarks>
/// Strategy: Iterative bitmask with pre-allocated buffer and streaming output.
/// Complexity: O(2^n); memory-efficient streaming generation.
/// Benchmark observations: Reduces memory allocations for large sequences; slightly slower than buffer-only variant for small inputs.
/// Advantages:
/// - Low memory usage with streaming
/// - Deterministic order of variations
/// Disadvantages:
/// - Slower for small input strings
/// Recommended usage:
/// - Very large inputs where in-memory generation is not feasible and streaming is required.
/// </remarks>
    public class BitmaskEfficientVariationYieldBufferGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();
        private readonly UmlautStackAllocConverter _converter = new();

        public IEnumerable<string> Generate(string input)
        {
            // Validate input first
            this.ValidateInput(input);

            if (string.IsNullOrWhiteSpace(input))
            {
                yield return input;
                yield break;
            }

            // Step 1: Pre-scan for branch points (same as yours)
            var positions = new List<(int index, char replacement)>();
            for (int i = 0; i < input.Length - 1; i++)
            {
                if (_helper.TryGetReplacement(input[i], input[i + 1], out var rep))
                {
                    positions.Add((i, rep));
                    i++;
                }
            }

            int k = positions.Count;
            int variationsCount = 1 << k;

            // Step 2: Create a reusable buffer for the variations
            // This lives outside the loop so we don't re-allocate it
            char[] buffer = new char[input.Length];

            for (int mask = 0; mask < variationsCount; mask++)
            {
                int srcIdx = 0;
                int destIdx = 0;
                int bitIdx = 0;

                // Step 3: Build the string by checking the mask
                while (srcIdx < input.Length)
                {
                    // If we are at a position that CAN be replaced
                    if (bitIdx < k && srcIdx == positions[bitIdx].index)
                    {
                        // Check the bit for this specific branch point
                        if ((mask & (1 << bitIdx)) != 0)
                        {
                            buffer[destIdx++] = positions[bitIdx].replacement; // AE -> Ä
                            srcIdx += 2;
                        }
                        else
                        {
                            buffer[destIdx++] = input[srcIdx++]; // AE -> AE (part 1)
                            buffer[destIdx++] = input[srcIdx++]; // AE -> AE (part 2)
                        }
                        bitIdx++;
                    }
                    else
                    {
                        // Standard character, just copy
                        buffer[destIdx++] = input[srcIdx++];
                    }
                }

                // Yield the specific slice of the buffer
                yield return new string(buffer, 0, destIdx);
            }
        }
    }
}
