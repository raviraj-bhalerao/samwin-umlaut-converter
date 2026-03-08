using BenchmarkDotNet.Attributes;
using Samwin.UmlautConverterLib.Step1;
using System.Collections.Generic;
using System.Linq;

[MemoryDiagnoser] // tracks memory allocations
public class UmlautConverterNamesBenchmark
{
    // Number of repetitions for input string
    [Params(1, 5)]
    public int RepeatCount;

    private IEnumerable<string> _input = null!;
    private UmlautSimpleConverter _simpleConverter = null!;
    private UmlautStackAllocConverter _stackAllocConverter = null!;
    private UmlautArrayConverter _arrayConverter = null!;
    private UmlautStringCreateConverter _stringCreateConverter = null!;
    private UmlautStackAllocOrRentedHeapConverter _stackAllocOrRentedHeapConverter = null!;
    private UmlautRentedHeapConverter _rentedHeapConverter = null!;

    [GlobalSetup]
    public void Setup()
    {
        var baseWord = "Gaensefuesschenaerzteausbildungsstaette";
        // Generate a "realistic" large input string
        // var baseWords = new[]
        // {
        //     "Gaensefuesschenaerzteausbildungsstaette"
        // };

        // Repeat each word sequence to generate very large string
        _input = Enumerable.Range(0, RepeatCount)
            .Select(_ => baseWord);

        _simpleConverter = new UmlautSimpleConverter();
        _stackAllocConverter = new UmlautStackAllocConverter();
        _arrayConverter = new UmlautArrayConverter();
        _stringCreateConverter = new UmlautStringCreateConverter();
        _stackAllocOrRentedHeapConverter = new UmlautStackAllocOrRentedHeapConverter();
        _rentedHeapConverter = new UmlautRentedHeapConverter();
    }

    [Benchmark(Baseline = true)] // baseline comparison
    public string SimpleConverter()
    {
        var result = _input.Select(word => _simpleConverter.Convert(word)).ToArray();
        return "";
    } 

    [Benchmark]
    public string ArrayConverter()
    {
        var result = _input.Select(word => _arrayConverter.Convert(word)).ToArray();
        return "";
    } 

    [Benchmark]
    public string StackAllocConverter() 
    {
        var result = _input.Select(word => _stackAllocConverter.Convert(word)).ToArray();
        return "";
    } 

    [Benchmark]
    public string RentedHeapConverter()
    {
        var result = _input.Select(word => _rentedHeapConverter.Convert(word)).ToArray();
        return "";
    } 

    [Benchmark]
    public string StackAllocOrRentedHeapConverter()
    {
        var result = _input.Select(word => _stackAllocOrRentedHeapConverter.Convert(word)).ToArray();
        return "";
    } 

    [Benchmark]
    public string StringCreateConverter()
    {
        var result = _input.Select(word => _stringCreateConverter.Convert(word)).ToArray();
        return "";
    }

}