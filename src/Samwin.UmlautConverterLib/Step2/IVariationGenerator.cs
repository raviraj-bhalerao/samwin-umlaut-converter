using System.Collections.Generic;
namespace Samwin.UmlautConverterLib.Step2
{
    public interface IVariationGenerator
    {
        /// <summary>
        /// Generates all possible variations of the input string based on replaceable pairs.
        /// </summary>
        IEnumerable<string> Generate(string input);
    }
}