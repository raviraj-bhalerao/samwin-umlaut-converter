using System;
using Samwin.UmlautConverterLib;
using Samwin.UmlautConverterLib.Step1;

class Program
{
    static void Main()
    {
        Console.WriteLine("Samwin Umlaut Converter - Sample Client\n");

        string[] inputs = { "Mueller", "Schroeder", "Aesch", "Oesterreich" };

        var simpleConverter = new UmlautSimpleConverter();
        var bufferConverter = new UmlautEfficientConverter();
        var ultraFastConverter = new UmlautMemoryOptimizedConverter();

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