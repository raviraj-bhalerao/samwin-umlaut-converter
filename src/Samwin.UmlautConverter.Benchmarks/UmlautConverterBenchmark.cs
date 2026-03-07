using BenchmarkDotNet.Attributes;
using Samwin.UmlautConverterLib;
using Samwin.UmlautConverterLib.Step1;
using System.Linq;

[MemoryDiagnoser] // tracks memory allocations
public class UmlautConverterBenchmark
{
    // Number of repetitions for input string
    [Params(1000, 10000, 100_000, 500_000, 1_000_000, 2_000_000)]
    public int RepeatCount;

    private string _input = null!;
    private UmlautSimpleConverter _simple = null!;
    private UmlautEfficientConverter _buffer = null!;
    private UmlautMemoryOptimizedConverter _memoryOptimized = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate a "realistic" large input string
        var baseWords = new[]
        {
        "Mueller", "Schroeder", "Aesch", "Oesterreich",
        "Ueber", "Goethe", "Fussball", "Kaese", "Schoen"
    };

        // Repeat each word sequence to generate very large string
        _input = string.Concat(Enumerable.Range(0, RepeatCount)
            .Select(_ => string.Join(" ", baseWords) + " "));

        _simple = new UmlautSimpleConverter();
        _buffer = new UmlautEfficientConverter();
        _memoryOptimized = new UmlautMemoryOptimizedConverter();
    }

    [Benchmark(Baseline = true)] // baseline comparison
    public string SimpleConverter() => _simple.Convert(_input);

    [Benchmark]
    public string BufferConverter() => _buffer.Convert(_input);

    [Benchmark]
    public string MemoryOptimizedConverter() => _memoryOptimized.Convert(_input);
}