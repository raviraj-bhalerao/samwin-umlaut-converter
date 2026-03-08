using System;
using System.Collections.Generic;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Simple, recursive generator producing all string variations from the ground up.
    /// </summary>
    /// <remarks>
    /// Strategy: Recursive, ground-up generation of variations.
    /// Complexity: O(2^n) in number of replaceable characters; memory allocations proportional to total variations.
    /// Benchmark observations: Slowest for larger inputs; memory allocations are high; baseline for comparison.
    /// Advantages:
    /// - Easy to understand and maintain
    /// - Produces all variations deterministically
    /// Disadvantages:
    /// - High memory usage
    /// - Poor performance for large inputs
    /// Recommended usage:
    /// - Small inputs such as short names or few replaceable characters.
    /// </remarks>
    public class SimpleGroundUpVariationGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();
        public IEnumerable<string> Generate(string input)
        {
            this.ValidateInput(input);
            
            if (string.IsNullOrWhiteSpace(input))
                return new List<string> { input };

            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            GenerateRecursive(input, 0, "", results);
            return new List<string>(results);
        }

        private void GenerateRecursive(string input, int index, string current, HashSet<string> results)
        {
            if (index >= input.Length)
            {
                results.Add(current);
                return;
            }

            bool replaced = false;
            if (index < input.Length - 1 &&
                _helper.TryGetReplacement(input[index], input[index + 1], out var rep))
            {
                // branch: keep original
                GenerateRecursive(input, index + 2, current + input.Substring(index, 2), results);
                // branch: replace
                GenerateRecursive(input, index + 2, current + rep, results);
                replaced = true;
            }

            if (!replaced)
                GenerateRecursive(input, index + 1, current + input[index], results);
        }
    }
}