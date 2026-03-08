using System;
using System.Linq;
using Samwin.UmlautConverterLib;
using Samwin.UmlautConverterLib.Step1;
using Samwin.UmlautConverterLib.Step2;

class Program
{
    static void Main()
    {

        var variationCoverter = new BitmaskEfficientVariationYieldGenerator();
        var result = variationCoverter.Generate("One Two Three Four").ToList();
        Console.WriteLine("Samwin Umlaut Converter - Sample Client\n");

        string[] inputs = { "Mueller", "Schroeder", "Aesch", "Oesterreich" };

        IUmlautConverter simpleConverter = new UmlautSimpleConverter();
        IUmlautConverter bufferConverter = new UmlautStackAllocConverter();
        IUmlautConverter ultraFastConverter = new UmlautStringCreateConverter();

        foreach (var input in inputs)
        {
            Console.WriteLine($"Input: {input}");

            Console.WriteLine($"  UmlautConverter:       {simpleConverter.Convert(input)}");
            Console.WriteLine($"  UmlautBufferConverter: {bufferConverter.Convert(input)}");
            Console.WriteLine($"  UmlautUltraFastConverter: {ultraFastConverter.Convert(input)}");

            Console.WriteLine();
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}