using System.Collections.Generic;
using Samwin.UmlautConverterLib.Step1;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
/// <summary>
/// Iterative bitmask-based generator producing all variations.
/// </summary>
/// <remarks>
/// Strategy: Iterative, bitmask representation for positions of changeable characters.
/// Complexity: O(2^n); memory proportional to number of variations.
/// Benchmark observations: Slightly slower than branching for small inputs; moderate memory usage.
/// Advantages:
/// - Avoids recursion stack overflow
/// - Deterministic ordering
/// Disadvantages:
/// - Slower than branching buffer for small inputs
/// Recommended usage:
/// - Medium-length strings or deep variation trees where recursion might fail.
/// </remarks>
    public class BitmaskEfficientVariationGenerator : VariationConverterBase, IVariationGenerator
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
                results.Add(variation);
            }

            return results;
        }
    }
}
