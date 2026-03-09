using System;
using System.Collections.Generic;
using System.Linq;
using Samwin.UmlautConverterLib.Step1;
using Xunit.Abstractions;

namespace Samwin.UmlautConverterLib.Step1.Tests
{
    public class UmlautConverterContractTests
    {
        public static IEnumerable<object[]> Generators()
        {
            yield return new object[] { new UmlautSimpleConverter() };
            yield return new object[] { new UmlautStackAllocConverter() };
            yield return new object[] { new UmlautArrayConverter() };
            yield return new object[] { new UmlautStringCreateConverter() };
            yield return new object[] { new UmlautStackAllocOrRentedHeapConverter() };
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldReturnEmptyString_WhenInputIsEmpty(IUmlautConverter converter)
        {
            var result = converter.Convert("");
            Assert.Equal("", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldReturnNull_WhenInputIsNull(IUmlautConverter converter)
        {
            string? input = null;
            var result = converter.Convert(input!); // depending on your null policy
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldReplaceSingleUmlaut(IUmlautConverter converter)
        {
            var result = converter.Convert("Mueller");
            Assert.Equal("Müller", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldReplaceMultipleUmlauts(IUmlautConverter converter)
        {
            var result = converter.Convert("Mueller und Schroeder");
            Assert.Equal("Müller und Schröder", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldNotChangeStringWithoutUmlauts(IUmlautConverter converter)
        {
            var result = converter.Convert("Hello World");
            Assert.Equal("Hello World", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldHandleConsecutivePatterns(IUmlautConverter converter)
        {
            var result = converter.Convert("aessss");
            Assert.Equal("äẞẞ", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldHandleUpperAndLowerCase(IUmlautConverter converter)
        {
            var result = converter.Convert("Aesch Oesterreich Ueber");
            Assert.Equal("Äsch Österreich Über", result);
        }

        [Theory]
        [MemberData(nameof(Generators))]
        public void Convert_ShouldHandleLongInputEfficiently(IUmlautConverter converter)
        {
            var input = string.Concat(Enumerable.Repeat("Mueller Schroeder ", 1000));
            var expected = string.Concat(Enumerable.Repeat("Müller Schröder ", 1000));

            var result = converter.Convert(input);

            Assert.Equal(expected, result);
        }
        [Theory]
        [MemberData(nameof(Generators))]
        public void Converter_Output(IUmlautConverter converter)
        {
            Console.WriteLine($"::group::{converter.GetType().Name}.SQLs in {nameof(UmlautConverterContractTests)}.{nameof(Converter_Output)}");
            try
            {
                var result = converter.Convert("KOESTNER");
                Console.WriteLine($"Input: KOESTNER, Output: {result}");
                result = converter.Convert("RUESSWURM");
                Console.WriteLine($"Input: RUESSWURM, Output: {result}");
            }
            finally
            {
                Console.WriteLine("::endgroup::");
            }
        }
    }
}