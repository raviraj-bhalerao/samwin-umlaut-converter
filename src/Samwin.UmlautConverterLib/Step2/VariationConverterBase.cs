using System;

namespace Samwin.UmlautConverterLib.Step2
{
    public abstract class VariationConverterBase
    {
        protected readonly int MaxLength;
        protected readonly int MaxWords;

        /// <summary>
        /// Default constructor sets default maximum input length and word count.
        /// </summary>
        protected VariationConverterBase(): this(50, 3)
        {
        }
        
        /// <summary>
        /// Constructor allows setting maximum input length and word count.
        /// </summary>
        protected VariationConverterBase(int maxLength = 50, int maxWords = 3)
        {
            MaxLength = maxLength;
            MaxWords = maxWords;
        }

        /// <summary>
        /// Validates the input string for length and number of words.
        /// Throws <see cref="ArgumentException"/> if validation fails.
        /// </summary>
        /// <param name="input">Input string to validate</param>
        protected void ValidateInput(string input)
        {
            if (input.Length > MaxLength)
                throw new ArgumentException($"Input too long. Max allowed length is {MaxLength}.");

            if (input.Split(' ').Length > MaxWords)
                throw new ArgumentException($"Too many words. Max allowed is {MaxWords}.");
        }

    }
}