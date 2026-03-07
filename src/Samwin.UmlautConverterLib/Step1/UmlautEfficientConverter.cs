using System;

namespace Samwin.UmlautConverterLib.Step1;

/// <summary>
/// A high-performance converter that processes the input string in a single pass.
/// </summary>
/// <remarks>
/// This implementation uses <see cref="ReadOnlySpan{T}"/> to avoid unnecessary
/// string allocations while scanning the input. A character buffer is used to
/// build the output string.
///
/// Complexity:
/// O(n)
/// where n = length of the input string.
///
/// Advantages:
/// - Single-pass processing
/// - Significantly fewer allocations than repeated Replace operations
/// - Fastest implementation in most benchmark scenarios
///
/// Disadvantages:
/// - Slightly more complex logic
/// - Less flexible for dynamically changing mappings
/// - Mapping logic is embedded in a switch statement
///
/// Note:
/// This implementation assumes replacement patterns of fixed length (2 characters).
/// Handling variable-length patterns would require additional logic.
/// </remarks>
public class UmlautEfficientConverter
{
    public string Convert(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Use ReadOnlySpan<char> to access the input string without copying it
        ReadOnlySpan<char> span = input.AsSpan();

        // Allocate a buffer equal to the input length.
        // Since all replacements are shorter or equal in length,
        // this buffer is guaranteed to be large enough.
        char[] buffer = new char[input.Length];

        int pos = 0;

        for (int i = 0; i < span.Length; i++)
        {
            if (i < span.Length - 1)
            {
                switch ((span[i], span[i + 1]))
                {
                    case ('a', 'e'): case ('a', 'E'): buffer[pos++] = 'ä'; i++; continue;
                    case ('A', 'e'): case ('A', 'E'): buffer[pos++] = 'Ä'; i++; continue;
                    case ('o', 'e'): case ('o', 'E'): buffer[pos++] = 'ö'; i++; continue;
                    case ('O', 'e'): case ('O', 'E'): buffer[pos++] = 'Ö'; i++; continue;
                    case ('u', 'e'): case ('u', 'E'): buffer[pos++] = 'ü'; i++; continue;
                    case ('U', 'e'): case ('U', 'E'): buffer[pos++] = 'Ü'; i++; continue;
                    case ('s', 's'): case ('S', 'S'): buffer[pos++] = 'ß'; i++; continue;
                }
            }

            buffer[pos++] = span[i];
        }

        // Create the final string from the buffer using only the filled portion
        return new string(buffer, 0, pos);
    }
}