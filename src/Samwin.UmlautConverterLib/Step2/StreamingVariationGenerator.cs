using System.Collections.Generic;
using Samwin.UmlautConverterLib.Step1;
using Samwin.UmlautConverterLib.Utils;

namespace Samwin.UmlautConverterLib.Step2
{
    /// <summary>
    /// Lazily generates string variations using streaming (IEnumerable/yield).
    /// </summary>
    /// <remarks>
    /// Strategy: Lazy evaluation, yields variations one at a time.
    /// Complexity: O(2^n) in variations; memory usage is higher than buffer-based due to yield overhead.
    /// Benchmark observations: ~2x slower than simple converter; memory allocations double for small inputs; avoids holding all variations in memory at once.
    /// Advantages:
    /// - Memory-efficient for very large sequences
    /// - Can start processing results immediately
    /// Disadvantages:
    /// - Slower than buffer-based approaches
    /// - Higher memory allocations for small inputs
    /// Recommended usage:
    /// - Large input sequences where variations are processed on-the-fly.
    /// </remarks>
    public class StreamingVariationGenerator : VariationConverterBase, IVariationGenerator
    {
        private readonly UmlautMappingHelper _helper = new();
        private readonly UmlautStackAllocConverter _converter = new();

        public IEnumerable<string> Generate(string input)
        {
            this.ValidateInput(input);
            
            if (string.IsNullOrWhiteSpace(input))
            {
                yield return input;
                yield break;
            }

            foreach (var v in GenerateRecursive(input, 0, ""))
                // yield return _converter.Convert(v);
                yield return v;
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
                _helper.TryGetReplacement(input[index], input[index + 1], out var rep))
            {
                foreach (var v in GenerateRecursive(input, index + 2, current + input.Substring(index, 2)))
                    yield return v;
                foreach (var v in GenerateRecursive(input, index + 2, current + rep))
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