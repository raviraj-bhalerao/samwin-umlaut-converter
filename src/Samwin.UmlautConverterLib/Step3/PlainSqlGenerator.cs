using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Samwin.UmlautConverterLib.Step2;

namespace Samwin.UmlautConverterLib.Step3
{
    /// <summary>
    /// Generates plain SQL statements with values embedded for names with umlaut variations.
    /// </summary>
    /// <remarks>
    /// The `PlainSqlGenerator` produces literal SQL queries by formatting each variation using an injected SQL value formatter.
    /// Each input name is expanded into all possible umlaut variations.
    ///
    /// Complexity:
    /// O(n × m)
    /// where:
    /// - n = number of input names
    /// - m = average number of variations per name
    ///
    /// Benchmark observations:
    /// - Simpler than parameterized version for quick searches
    /// - Slightly less safe due to embedded values; ensure proper escaping
    ///
    /// Advantages:
    /// - Easy to read SQL for debugging
    /// - Works well for batch scripts or ad-hoc queries
    ///
    /// Disadvantages:
    /// - SQL injection risk if values are not properly escaped
    /// - Repeated strings may increase memory usage for very large input sets
    ///
    /// Recommended usage:
    /// When SQL queries are generated for direct execution and readability is desired.
    /// </remarks>
    public class PlainSqlGenerator : ISqlQueryGenerator<string>
    {
        private readonly IVariationGenerator _variationGenerator;
        private readonly ISqlValueFormatter _formatter;

        public PlainSqlGenerator() : this(new BranchingVariationBufferGenerator(), new StandardSqlValueFormatter()) //default to a good performing generator, but allow injection of any generator for testing or experimentation
        {

        }
        public PlainSqlGenerator(
                IVariationGenerator variationGenerator,
                ISqlValueFormatter formatter)
        {
            _variationGenerator = variationGenerator;
            _formatter = formatter;
        }

        public IEnumerable<string> Generate(IEnumerable<string> names, bool combineAll = false)
        {
            if (combineAll)
            {
                var allVariations = names
                    .SelectMany(n => _variationGenerator.Generate(n))
                    .Distinct()
                    .Select(v => _formatter.Format(v));

                yield return SqlHelpers.PlainSelectStatemement(string.Join(", ", allVariations));
            }
            else
            {
                foreach (var name in names)
                {
                    var values = _variationGenerator
                        .Generate(name)
                        .Distinct()
                        .Select(v => _formatter.Format(v));

                    yield return SqlHelpers.PlainSelectStatemement(string.Join(", ", values));
                }
            }
        }
    }
}
