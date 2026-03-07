using System.Collections.Generic;

namespace Samwin.UmlautConverterLib.Step1;

/// <summary>
/// A simple implementation of the umlaut converter using repeated string replacement.
/// </summary>
/// <remarks>
/// Each mapping is applied using <see cref="string.Replace(string, string)"/>.
/// Since strings in .NET are immutable, every Replace call creates a new string instance.
/// 
/// Complexity:
/// O(n × k)
/// where:
/// n = length of the input string
/// k = number of mappings (7 in this case)
///
/// Advantages:
/// - Very simple and easy to understand
/// - Easy to maintain and extend
///
/// Disadvantages:
/// - Multiple passes over the string
/// - Creates intermediate string allocations
/// - Less efficient for large input strings
/// </remarks>
public class UmlautSimpleConverter
{
private static readonly Dictionary<string, string> _umlautMappings = new()
{
    // Lowercase
    { "ae", "ä" },
    { "oe", "ö" },
    { "ue", "ü" },
    { "ss", "ß" },

    // Uppercase first-letter combinations
    { "Ae", "Ä" },
    { "Oe", "Ö" },
    { "Ue", "Ü" },

    // Fully uppercase (optional if needed)
    { "AE", "Ä" },
    { "OE", "Ö" },
    { "UE", "Ü" },
    { "SS", "ẞ" } // Capital ß exists in Unicode
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