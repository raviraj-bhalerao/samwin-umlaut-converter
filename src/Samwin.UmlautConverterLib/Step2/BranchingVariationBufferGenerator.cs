using System;
using System.Collections.Generic;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Generates all possible variations of a string by branching at each replaceable pair.
    /// Uses UmlautMappingHelper to apply replacements.
    /// </summary>
    /// <remarks>
    /// Complexity:
    /// O(n × 2^k)
    /// where n = length of the string, k = number of replaceable pairs.
    ///
    /// Advantages:
    /// - Easy to understand and explain (branching tree)
    /// - Generates all possible variations
    /// - Uses mapping helper directly, no extra string scanning needed
    ///
    /// Disadvantages:
    /// - Exponential growth with number of replaceable pairs
    /// - Recursion depth = number of replaceable pairs
    /// </remarks>
    public class BranchingVariationBufferGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();

        /// <summary>
        /// Generates all variations of the input string based on replaceable pairs.
        /// </summary>
        public IEnumerable<string> Generate(string input)
        {
            // Validate input first
            this.ValidateInput(input);

            if (string.IsNullOrEmpty(input)) return new List<string> { input };

            var variations = new List<string>();
            // Architectural Choice: Use a safe buffer size for names
            char[] buffer = new char[input.Length * 2]; // *2 just in case of weird expansions
            GenerateRecursive(input, 0, buffer, 0, variations);
            return variations;
        }
        private void GenerateRecursive(string input, int index, char[] buffer, int bufferPos, List<string> variations)
        {
            // Base Case: We hit the end
            if (index >= input.Length)
            {
                // Only allocate the string once per variation
                variations.Add(new string(buffer, 0, bufferPos));
                return;
            }

            // Check for a replacement
            if (index < input.Length - 1 && _helper.TryGetReplacement(input[index], input[index + 1], out var replacement))
            {
                // Branch 1: Keep original (AE -> AE)
                buffer[bufferPos] = input[index];
                buffer[bufferPos + 1] = input[index + 1];
                GenerateRecursive(input, index + 2, buffer, bufferPos + 2, variations);

                // Branch 2: Replace (AE -> Ä)
                buffer[bufferPos] = replacement;
                GenerateRecursive(input, index + 2, buffer, bufferPos + 1, variations);
            }
            else
            {
                // No replacement: Just copy current char
                buffer[bufferPos] = input[index];
                GenerateRecursive(input, index + 1, buffer, bufferPos + 1, variations);
            }
        }
    }
}