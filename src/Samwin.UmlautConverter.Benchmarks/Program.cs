using BenchmarkDotNet.Running;

class Program
{
    static void Main(string[] args)
    {
        // Runs all benchmarks in the UmlautConverterBenchmark class
        var summary = BenchmarkRunner.Run<UmlautConverterBenchmark>();
    }
}