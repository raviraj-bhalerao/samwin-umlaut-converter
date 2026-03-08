using BenchmarkDotNet.Attributes;
using Samwin.UmlautConverterLib.Step1;
using System.Linq;

[MemoryDiagnoser] // tracks memory allocations
public class UmlautConverterLongTextBenchmark
{
    // Number of repetitions for input string
    [Params(1000, 10000, 100_000, 500_000, 1_000_000, 2_000_000)]
    public int RepeatCount;

    private string _input = null!;
    private UmlautSimpleConverter _simpleConverter = null!;
    // private UmlautStackAllocConverter _stackAllocConverter = null!;
    private UmlautArrayConverter _arrayConverter = null!;
    private UmlautStringCreateConverter _stringCreateConverter = null!;
    private UmlautStackAllocOrRentedHeapConverter _stackAllocOrRentedHeapConverter = null!;
    private UmlautRentedHeapConverter _rentedHeapConverter = null!;

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

        _simpleConverter = new UmlautSimpleConverter();
        // _stackAllocConverter = new UmlautStackAllocConverter(); // Not suitable for very large input due to stack overflow risk
        _arrayConverter = new UmlautArrayConverter();
        _stringCreateConverter = new UmlautStringCreateConverter();
        _stackAllocOrRentedHeapConverter = new UmlautStackAllocOrRentedHeapConverter();
        _rentedHeapConverter = new UmlautRentedHeapConverter();
    }

    [Benchmark(Baseline = true)] // baseline comparison
    public string SimpleConverter() => _simpleConverter.Convert(_input);

    // [Benchmark]
    // public string StackAllocConverter() => _stackAllocConverter.Convert(_input);

    [Benchmark]
    public string ArrayConverter() => _arrayConverter.Convert(_input);

    [Benchmark]
    public string RentedHeapConverter() => _rentedHeapConverter.Convert(_input);

    [Benchmark]
    public string StackAllocOrRentedHeapConverter() => _stackAllocOrRentedHeapConverter.Convert(_input);
    [Benchmark]
    public string StringCreateConverter() => _stringCreateConverter.Convert(_input);

}