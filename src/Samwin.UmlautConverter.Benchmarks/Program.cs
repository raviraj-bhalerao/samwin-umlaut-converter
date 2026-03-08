using BenchmarkDotNet.Running;

class Program
{
    static void Main(string[] args)
    {
        // Runs all benchmarks in the UmlautConverterBenchmark class
        var summaryStep1Names = BenchmarkRunner.Run<UmlautConverterNamesBenchmark>();
        var summaryStep1LongText = BenchmarkRunner.Run<UmlautConverterLongTextBenchmark>();
        var summaryStep2 = BenchmarkRunner.Run<VariationConverterBenchmark>();
    }
}