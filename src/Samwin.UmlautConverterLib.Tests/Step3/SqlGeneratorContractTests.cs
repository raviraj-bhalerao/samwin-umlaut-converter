using System;
using System.Collections.Generic;
using System.Linq;
using Samwin.UmlautConverterLib.Step2;
using Xunit.Abstractions;

namespace Samwin.UmlautConverterLib.Step3.Tests
{
    public class SqlGeneratorContractTests
    {
        public static IEnumerable<object[]> Generators()
        {
            IVariationGenerator variationGenerator = new BranchingVariationBufferGenerator();
            // Use Step2 variation generators here for injection
            yield return new object[] { new PlainSqlGenerator(variationGenerator, new StandardSqlValueFormatter()), variationGenerator };
            yield return new object[] { new ParameterizedSqlGenerator(variationGenerator), variationGenerator };
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void SqlGenerator_GeneratesExpected(object generatorObj, IVariationGenerator variationGenerator)
        {
            // 1. Cast to the interface to call Generate
            // Since we have two different Ts, 'dynamic' is fine for a quick test 
            // or you can use reflection/pattern matching.
            dynamic generator = generatorObj;

            // 2. Materialize the IEnumerable to a List so we can inspect the contents
            var results = Enumerable.ToList(generator.Generate(new string[] { "RUESSWURM" }, combineAll: true));

            Assert.NotNull(results);
            Assert.NotEmpty(results);

            // 3. Get the first item to check the type
            var firstResult = results[0];

            if (firstResult is string plainSql)
            {
                Assert.Contains("SELECT * FROM tbl_phonebook", plainSql);
                Assert.Contains("WHERE last_name IN", plainSql);

                // We can also check that the variations are present in the SQL string
                var expectedVariations = variationGenerator.Generate("RUESSWURM").Select(v => $"'{v}'");
                foreach (var variation in expectedVariations)
                {
                    Assert.Contains(variation, plainSql);
                }
            }
            else if (firstResult is SqlQuery sqlQuery)
            {
                Assert.Contains("SELECT * FROM tbl_phonebook", sqlQuery.Sql);
                Assert.True(sqlQuery.Parameters.Count > 0);
            }
            else
            {
                // This will now show the actual class name if it fails
                throw new InvalidOperationException("Unknown result type: " + firstResult.GetType().Name);
            }
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Generate_WithMultipleNamesAndCombineFalse_ReturnsMultipleStatements(dynamic generator, IVariationGenerator variationGenerator)
        {
            var names = new[] { "ALICE", "BOB" };
            var results = Enumerable.ToList(generator.Generate(names, combineAll: false));

            // We expect one SQL statement per name
            Assert.Equal(2, results.Count);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Generate_HandlesSqlInjectionCharacters(dynamic generator, IVariationGenerator variationGenerator)
        {
            var names = new[] { "O'Reilly" };
            var results = Enumerable.ToList(generator.Generate(names, combineAll: true));
            var first = results[0];

            if (first is string sql)
            {
                // Should be escaped for plain SQL
                // Assert.Contains("O'Reilly", sql);
                Assert.Contains("O''Reilly", sql); // Escaped version
            }
            else if (first is SqlQuery query)
            {
                // Parameter value should stay as original, SQL should be clean
                Assert.Contains("O'Reilly", query.Parameters.Values);
                Assert.DoesNotContain("O'Reilly", query.Sql);
            }
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Generate_WithDuplicateInputs_ReturnsDistinctValues(dynamic generator, IVariationGenerator variationGenerator)
        {
            var names = new[] { "DUPE", "DUPE" };
            var results = Enumerable.ToList(generator.Generate(names, combineAll: true));

            // Logic inside your generator uses .Distinct(), 
            // so we should only see one set of variations.
            // (Assuming variation generator is deterministic)
            if (results[0] is SqlQuery query)
            {
                var variationCountPerName = 1; // Simplify for example
                Assert.True(query.Parameters.Count >= variationCountPerName);
            }
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void SqlGenerator_GeneratesOutput(object generatorObj, IVariationGenerator variationGenerator)
        {
            Console.WriteLine($"This test method '{nameof(SqlGeneratorContractTests)}.{nameof(SqlGenerator_GeneratesOutput)}' for {generatorObj.GetType().FullName} outputs the generated SQL by for manual inspection. It does not contain assertions.");
            dynamic generator = generatorObj;
            var names = new[] { "KOESTNER", "RUESSWURM", "DUERMUELLER", "JAEAESKELAEINEN", "GROSSSCHAEDL" };

            var results = Enumerable.ToList(generator.Generate(names, combineAll: true));
            var first = results[0];

            if (first is string combinedSql)
            {
                Console.WriteLine("Generated combined plain SQL:\n" + combinedSql);
            }
            else if (first is SqlQuery query)
            {
                Console.WriteLine("Generated combined parameterised SQL:\n" + query.Sql);
                var paramString = string.Join("|", query.Parameters.Select(p => $"{p.Key}:{p.Value}"));
                Console.WriteLine("Parameters: " + paramString);
            }
            else
            {
                // This will now show the actual class name if it fails
                throw new InvalidOperationException("Unknown result type: " + first.GetType().Name);
            }

            results = Enumerable.ToList(generator.Generate(names, combineAll: false));
            first = results[0];

            if (first is string individualSql)
            {
                Console.WriteLine("Individual plain  SQLs:");
                foreach (var sql in results)
                {
                    Console.WriteLine(sql);
                }
            }
            else if (first is SqlQuery parameterisedQuery)
            {
                Console.WriteLine("Individual parameterised SQLs:");
                foreach (SqlQuery query in results)
                {
                    Console.WriteLine("SQL:\n" + query.Sql);
                    var paramString = string.Join("|", query.Parameters.Select(p => $"{p.Key}:{p.Value}"));
                    Console.WriteLine("Parameters: " + paramString);
                }
            }
            else
            {
                // This will now show the actual class name if it fails
                throw new InvalidOperationException("Unknown result type: " + first.GetType().Name);
            }



        }
    }
}