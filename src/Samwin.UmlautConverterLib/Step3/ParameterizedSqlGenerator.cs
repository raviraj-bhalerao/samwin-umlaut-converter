using System.Collections.Generic;
using System.Linq;
using Samwin.UmlautConverterLib.Step2;

namespace Samwin.UmlautConverterLib.Step3
{
    /// <summary>
    /// Generates SQL statements to search for names with possible umlaut variations.
    /// </summary>
    /// <remarks>
    /// The `ParameterizedSqlGenerator` produces parameterized SQL queries using an injected variation generator.
    /// Each input name is expanded into all possible umlaut variations, and each variation is mapped to a unique SQL parameter.
    ///
    /// Complexity:
    /// O(n × m)
    /// where:
    /// - n = number of input names
    /// - m = average number of variations per name
    ///
    /// Benchmark observations:
    /// - Efficient for small to medium input sets
    /// - Parameterized queries prevent SQL injection and improve database performance
    ///
    /// Advantages:
    /// - Safe SQL with parameters
    /// - Reuses variation generators from Step 2
    /// - Supports combining all names into a single query or separate queries per name
    ///
    /// Disadvantages:
    /// - Slight overhead due to parameter dictionary construction
    ///
    /// Recommended usage:
    /// When integrating umlaut variation searches into database operations with parameterized SQL.
    /// </remarks>
    public class ParameterizedSqlGenerator : ISqlQueryGenerator<SqlQuery>
    {
        private readonly IVariationGenerator _variationGenerator;

        public ParameterizedSqlGenerator() : this(new BranchingVariationBufferGenerator()) //default to a good performing generator, but allow injection of any generator for testing or experimentation
        {

        }
        public ParameterizedSqlGenerator(IVariationGenerator variationGenerator)
        {
            _variationGenerator = variationGenerator;
        }

        public IEnumerable<SqlQuery> Generate(IEnumerable<string> names, bool combineAll = false)
        {

            if (combineAll)
            {
                // Combine all variations from all names into one SQL
                var allVariations = names
                    .SelectMany(n => _variationGenerator.Generate(n))
                    .Distinct()
                    .Select(v => $"{v}")
                    .ToList();

                var parameters = new Dictionary<string, object>();
                var placeholders = new List<string>();

                for (int i = 0; i < allVariations.Count; i++)
                {
                    var param = $"@p{i}";
                    placeholders.Add(param);
                    parameters[param] = allVariations[i];
                }

                var sql = SqlHelpers.PlainSelectStatemement(string.Join(", ", placeholders));

                yield return new SqlQuery
                {
                    Sql = sql,
                    Parameters = parameters
                };
            }
            else
            {
                // Generate one SQL per name
                foreach (var name in names)
                {
                    var variations = _variationGenerator.Generate(name).Distinct().ToList();

                    var parameters = new Dictionary<string, object>();
                    var placeholders = new List<string>();

                    for (int i = 0; i < variations.Count; i++)
                    {
                        var param = $"@p{i}";
                        placeholders.Add(param);
                        parameters[param] = variations[i];
                    }

                    var sql = SqlHelpers.PlainSelectStatemement(string.Join(", ", placeholders));

                    yield return new SqlQuery
                    {
                        Sql = sql,
                        Parameters = parameters
                    };
                }
            }


        }
    }
}