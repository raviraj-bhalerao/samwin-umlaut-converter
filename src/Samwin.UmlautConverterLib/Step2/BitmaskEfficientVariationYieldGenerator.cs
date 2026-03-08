using System.Collections.Generic;
using Samwin.UmlautConverterLib.Step1;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Streaming (yield) version of bitmask generator.
    /// </summary>
    /// <remarks>
    /// Strategy: Iterative bitmask generator using lazy evaluation (yield).
    /// Complexity: O(2^n); memory proportional to current variation only.
    /// Benchmark observations: Minor overhead vs non-yield bitmask; memory-efficient for large sequences.
    /// Advantages:
    /// - Lazy evaluation reduces memory footprint
    /// Disadvantages:
    /// - Slower than non-yield versions for small inputs
    /// Recommended usage:
    /// - Large sequences where full in-memory generation is not possible.
    /// </remarks>
    public class BitmaskEfficientVariationYieldGenerator : VariationConverterBase, IVariationGenerator
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
            var results = new List<string>();

            for (int mask = 0; mask < (1 << k); mask++)
            {
                var buffer = input.ToCharArray();
                for (int bit = 0; bit < k; bit++)
                {
                    if ((mask & (1 << bit)) != 0)
                    {
                        var (index, replacement) = positions[bit];
                        buffer[index] = replacement;
                        buffer[index + 1] = '\0';
                    }
                }

                string variation = new string(buffer).Replace("\0", "");
                // results.Add(_converter.Convert(variation));
                yield return variation;
            }
        }
    }
}
