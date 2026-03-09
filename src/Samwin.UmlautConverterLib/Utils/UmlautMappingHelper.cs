// UmlautMappingHelper.cs
using System;
using System.Collections.Generic;

namespace Samwin.UmlautConverterLib.Utils
{
    /// <summary>
    /// Centralized helper for all 2-character umlaut mappings.
    /// </summary>
    /// <remarks>
    /// Provides a single source of truth for both converters and variation generators.
    /// Reduces duplication and makes future extensions simple.
    /// </remarks>
    public class UmlautMappingHelper
    {
        private readonly Dictionary<(char, char), char> _umlautMappingsChar = new()
        {
            // Lowercase
            { ('a', 'e'), 'ä' },
            { ('o', 'e'), 'ö' },
            { ('u', 'e'), 'ü' },
            { ('s', 's'), 'ẞ' },

            // Mixed-case first letter
            { ('A', 'e'), 'Ä' },
            { ('O', 'e'), 'Ö' },
            { ('U', 'e'), 'Ü' },
            { ('S', 's'), 'ß' },


            // Fully uppercase
            { ('A', 'E'), 'Ä' },
            { ('O', 'E'), 'Ö' },
            { ('U', 'E'), 'Ü' },
            { ('S', 'S'), 'ß' } // optional
        };

        public bool TryGetReplacement(char first, char second, out char replacement)
        {
            return _umlautMappingsChar.TryGetValue((first, second), out replacement);
        }
        // Make this internal or public so the Converter can see it
        public IReadOnlyDictionary<(char First, char Second), char> Mappings => _umlautMappingsChar;
    }
}