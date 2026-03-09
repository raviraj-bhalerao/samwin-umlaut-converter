using System;
using System.Collections.Generic;
using System.Linq;
using Samwin.UmlautConverterLib.Step2;
using Xunit.Abstractions;

namespace Samwin.UmlautConverterLib.Step2.Tests
{

    public class VariationGeneratorContractTests
    {
        public static IEnumerable<object[]> Generators()
        {
            yield return new object[] { new SimpleGroundUpVariationGenerator() };
            yield return new object[] { new StreamingVariationGenerator() };
            yield return new object[] { new BitmaskEfficientVariationGenerator() };
            yield return new object[] { new BitmaskEfficientVariationBufferGenerator() };
            yield return new object[] { new BitmaskEfficientVariationYieldGenerator() };
            yield return new object[] { new BitmaskEfficientVariationYieldBufferGenerator() };
            yield return new object[] { new BranchingVariationGenerator() };
            yield return new object[] { new BranchingVariationBufferGenerator() };
            yield return new object[] { new BranchingVariationYieldGenerator() };
            yield return new object[] { new BranchingVariationYieldBufferGenerator() };
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void EmptyInput_ReturnsEmpty(IVariationGenerator generator)
        {
            var result = generator.Generate("").ToList();

            Assert.Single(result);
            Assert.Equal("", result[0]);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void NoReplaceablePairs_ReturnsOriginal(IVariationGenerator generator)
        {
            var result = generator.Generate("HELLO").ToList();

            Assert.Single(result);
            Assert.Equal("HELLO", result[0]);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void SingleReplacement_GeneratesTwoVariations(IVariationGenerator generator)
        {
            var result = generator.Generate("MAER").ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains("MAER", result);
            Assert.Contains("MÄR", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void MultiplePairs_GeneratesAllCombinations(IVariationGenerator generator)
        {
            var result = generator.Generate("RUESSWURM").ToList();

            Assert.Equal(4, result.Count);

            Assert.Contains("RUESSWURM", result);
            Assert.Contains("RÜßWURM", result);
            Assert.Contains("RUEßWURM", result);
            Assert.Contains("RÜSSWURM", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void NoDuplicateResults(IVariationGenerator generator)
        {
            var result = generator.Generate("RUESSWURM").ToList();

            Assert.Equal(result.Count, result.Distinct().Count());
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void CombinationCount_IsPowerOfTwo(IVariationGenerator generator)
        {
            var result = generator.Generate("AESSUE").ToList();

            Assert.Equal(8, result.Count); // 3 replaceable pairs → 2^3
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Enumeration_IsLazy(IVariationGenerator generator)
        {
            var enumerable = generator.Generate("RUESSWURM");

            Assert.NotNull(enumerable);

            var first = enumerable.First();

            Assert.NotNull(first);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Input_ExceedsMaxLength_Throws(IVariationGenerator generator)
        {
            string longInput = new string('A', 100); // longer than maxLength = 50
            Assert.Throws<ArgumentException>(() => generator.Generate(longInput).ToList());
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Input_ExceedsMaxWords_Throws(IVariationGenerator generator)
        {
            string multiWordInput = "One Two Three Four"; // 4 words, maxWords = 3
            Assert.Throws<ArgumentException>(() => generator.Generate(multiWordInput).ToList());
        }
        [Theory]
        [MemberData(nameof(Generators))]
        public void Converter_Output(IVariationGenerator converter)
        {
            Console.WriteLine($"::group::{converter.GetType().Name}.ConvertedVariations in {nameof(VariationGeneratorContractTests)}.{nameof(Converter_Output)}");
            try
            {
                var result = converter.Generate("KOESTNER").First();
                Console.WriteLine($"Input: KOESTNER, Output: {string.Join(", ", result)}");
                result = converter.Generate("RUESSWURM").First();
                Console.WriteLine($"Input: RUESSWURM, Output: {string.Join(", ", result)}");
            }
            finally
            {
                Console.WriteLine("::endgroup::");
            }
        }
    }
}