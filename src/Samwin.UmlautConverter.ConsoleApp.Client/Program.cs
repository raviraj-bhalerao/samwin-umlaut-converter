using System;
using System.Linq;
using Samwin.UmlautConverterLib.Step1;
using Samwin.UmlautConverterLib.Step2;
using Samwin.UmlautConverterLib.Step3;

class Program
{
    static void Main()
    {

        // var variationCoverter = new BitmaskEfficientVariationYieldGenerator();
        // var result = variationCoverter.Generate("One Two Three Four").ToList();
        // Console.WriteLine("Samwin Umlaut Converter - Sample Client\n");

        var names = new[] { "KOESTNER", "RUESSWURM", "DUERMUELLER", "JAEAESKELAEINEN", "GROSSSCHAEDL" };
        var paramSqlGenerator = new ParameterizedSqlGenerator(new BranchingVariationBufferGenerator());
        var paramSqlQueries = paramSqlGenerator.Generate(names, combineAll: true).ToList();
        System.Console.WriteLine("Combined parameterised SQL:");
        System.Console.WriteLine(paramSqlQueries[0].Sql);

        paramSqlQueries = paramSqlGenerator.Generate(names, combineAll: false).ToList();
        System.Console.WriteLine("Individual parameterised SQL:");
        foreach (var query in paramSqlQueries)
        {
            System.Console.WriteLine(query.Sql);
        }


        var sqlGenerator = new PlainSqlGenerator(new BranchingVariationBufferGenerator(), new StandardSqlValueFormatter());
        var sqlQueries = sqlGenerator.Generate(names, combineAll: true).ToList();
        System.Console.WriteLine("Combined plain SQL:");
        System.Console.WriteLine(sqlQueries[0]);

        sqlQueries = sqlGenerator.Generate(names, combineAll: false).ToList();
        System.Console.WriteLine("Individual plain  SQL:");
        foreach (var query in sqlQueries)
        {
            System.Console.WriteLine(query);
        }

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