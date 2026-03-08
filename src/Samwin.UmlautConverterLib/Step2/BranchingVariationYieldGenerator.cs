using System;
using System.Collections.Generic;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Streaming variant of branching generator using yield.
    /// </summary>
    /// <remarks>
    /// Strategy: Recursive branching with lazy evaluation (IEnumerable/yield).
    /// Complexity: O(2^n); memory proportional to call stack depth + current variation.
    /// Benchmark observations: Slightly slower than buffer-based branching; low memory usage for large inputs.
    /// Advantages:
    /// - Lazily produces variations
    /// - Low memory footprint for large input sequences
    /// Disadvantages:
    /// - Slower than buffer-based approaches for small inputs
    /// Recommended usage:
    /// - Large inputs where holding all variations in memory is not feasible.
    /// </remarks>
    public class BranchingVariationYieldGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();

        /// <summary>
        /// Generates all variations of the input string based on replaceable pairs.
        /// </summary>
        public IEnumerable<string> Generate(string input)
        {
            // Validate input first
            this.ValidateInput(input);

            if (string.IsNullOrEmpty(input))
            {
                yield return input;
                yield break;
            }

            foreach (var variation in GenerateRecursive(input, 0, ""))
                yield return variation;
        }

        private IEnumerable<string> GenerateRecursive(string input, int index, string current)
        {
            if (index >= input.Length)
            {
                yield return current;
                yield break;
            }

            bool replaced = false;

            if (index < input.Length - 1 &&
                _helper.TryGetReplacement(input[index], input[index + 1], out var replacement))
            {
                // Branch 1: keep original pair
                foreach (var v in GenerateRecursive(input, index + 2, current + input[index] + input[index + 1]))
                    yield return v;

                // Branch 2: replace with umlaut
                foreach (var v in GenerateRecursive(input, index + 2, current + replacement))
                    yield return v;

                replaced = true;
            }

            if (!replaced)
            {
                foreach (var v in GenerateRecursive(input, index + 1, current + input[index]))
                    yield return v;
            }
        }
    }
}