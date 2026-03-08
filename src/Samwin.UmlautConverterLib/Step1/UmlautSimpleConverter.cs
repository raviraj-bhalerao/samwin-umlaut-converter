using System.Collections.Generic;

namespace Samwin.UmlautConverterLib.Step1
{
    /// <summary>
    /// A straightforward implementation that converts replacement sequences
    /// (AE, OE, UE, SS, etc.) to their umlaut counterparts using repeated
    /// <see cref="string.Replace(string, string)"/> calls.
    /// </summary>
    /// <remarks>
    /// This approach performs one Replace operation per mapping.
    /// Since .NET strings are immutable, each Replace creates a new string instance.
    ///
    /// Complexity:
    /// O(n × k)
    /// where:
    /// n = length of the input string
    /// k = number of mappings.
    ///
    /// Benchmark observations:
    /// - Fastest implementation for short inputs such as personal names.
    /// - Benefits from highly optimized runtime implementations of Replace.
    /// - Produces significantly more memory allocations than single-pass converters.
    ///
    /// Advantages:
    /// - Very simple and easy to understand
    /// - Minimal code complexity
    /// - Fast for small inputs
    ///
    /// Disadvantages:
    /// - Multiple passes over the string
    /// - Higher GC pressure for large inputs
    ///
    /// Recommended usage:
    /// Best suited for short strings (e.g. names in the phonebook scenario).
    /// </remarks>
    public class UmlautSimpleConverter : IUmlautConverter
    {
        private static readonly Dictionary<string, string> _umlautMappings = new()
        {
            // Lowercase
            { "ae", "ä" },
            { "oe", "ö" },
            { "ue", "ü" },
            { "ss", "ẞ" },

            // Uppercase first-letter combinations
            { "Ae", "Ä" },
            { "Oe", "Ö" },
            { "Ue", "Ü" },

            // Fully uppercase (optional if needed)
            { "AE", "Ä" },
            { "OE", "Ö" },
            { "UE", "Ü" },
            { "SS", "ß" } // Capital ß exists in Unicode
        };

        /// <summary>
        /// Converts umlaut character sequences in the input string.
        /// </summary>
        public string Convert(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var result = input;

            foreach (var mapping in _umlautMappings)
            {
                result = result.Replace(mapping.Key, mapping.Value);
            }

            return result;
        }
    }
}