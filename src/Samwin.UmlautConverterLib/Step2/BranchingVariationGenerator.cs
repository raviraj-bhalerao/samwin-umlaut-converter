using System;
using System.Collections.Generic;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Recursive branching generator producing all variations in memory.
    /// </summary>
    /// <remarks>
    /// Strategy: Recursive branching approach using pre-allocated strings.
    /// Complexity: O(2^n); memory allocations low for small-to-medium inputs.
    /// Benchmark observations: Fastest among non-buffer approaches; very low memory allocations.
    /// Advantages:
    /// - Very fast for small/medium inputs
    /// - Low memory usage
    /// Disadvantages:
    /// - May hit stack limits for very deep recursion (long strings)
    /// Recommended usage:
    /// - Step2 scenarios with short names or word lists.
    /// </remarks>
    public class BranchingVariationGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();

        public IEnumerable<string> Generate(string input)
        {
            // Validate input first
            this.ValidateInput(input);

            if (string.IsNullOrEmpty(input)) return new List<string> { input };

            var variations = new List<string>();
            // Start the recursion with an empty "current" string
            GenerateRecursive(input, 0, string.Empty, variations);
            return variations;
        }

        private void GenerateRecursive(string input, int index, string current, List<string> variations)
        {
            // Base Case: We've reached the end of the input
            if (index >= input.Length)
            {
                variations.Add(current);
                return;
            }

            // Check for a replacement opportunity
            if (index < input.Length - 1 && _helper.TryGetReplacement(input[index], input[index + 1], out var replacement))
            {
                // Branch 1: Keep the original pair (e.g., AE)
                // Note: This creates a new string object on the heap
                GenerateRecursive(input, index + 2, current + input[index] + input[index + 1], variations);

                // Branch 2: Replace with the umlaut (e.g., Ä)
                // Note: This also creates a new string object on the heap
                GenerateRecursive(input, index + 2, current + replacement, variations);
            }
            else
            {
                // No replacement possible: just append the current character
                GenerateRecursive(input, index + 1, current + input[index], variations);
            }
        }
    }
}