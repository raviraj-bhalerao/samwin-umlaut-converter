using BenchmarkDotNet.Attributes;
using Samwin.UmlautConverterLib;
using Samwin.UmlautConverterLib.Step2;
using System.Collections.Generic;
using System.Linq;

[MemoryDiagnoser] // tracks memory allocations
public class VariationConverterBenchmark
{
    // Number of repetitions for input string
    [Params(1, 10, 100)]
    public int RepeatCount;

    private IEnumerable<string> _input = null!;
    private IVariationGenerator _bitmask = null!;
    private IVariationGenerator _bitmaskBuffer = null!;
    private IVariationGenerator _bitmaskYield = null!;
    private IVariationGenerator _bitmaskYieldBuffer = null!;
    private IVariationGenerator _branching = null!;
    private IVariationGenerator _branchingBuffer = null!;
    private IVariationGenerator _branchingYield = null!;
    private IVariationGenerator _branchingYieldBuffer = null!;
    private IVariationGenerator _simple = null!;
    private IVariationGenerator _streaming = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate a "realistic" large input string
        var baseWords = new[]
        {
            "Mueller", "Schroeder", "Aesch", "Oesterreich",
            "Ueber", "Goethe", "Fussball", "Kaese", "Schoen",
            "Gaensefuesschenaerzteausbildungsstaette", "RUESSWURM"
        };
        // var baseWords = new[]
        // {
        //     "Mueller", "Schroeder", "Aesch", "Oesterreich"
        // };
        // Repeat each word sequence to generate very large string
        _input = baseWords.SelectMany(word => Enumerable.Repeat(word, RepeatCount));

        _bitmask = new BitmaskEfficientVariationGenerator();
        _bitmaskBuffer = new BitmaskEfficientVariationBufferGenerator();
        _bitmaskYield = new BitmaskEfficientVariationYieldGenerator();
        _bitmaskYieldBuffer = new BitmaskEfficientVariationYieldBufferGenerator();
        _branching = new BranchingVariationBufferGenerator();
        _branchingBuffer = new BranchingVariationBufferGenerator();
        _branchingYield = new BranchingVariationYieldGenerator();
        _branchingYieldBuffer = new BranchingVariationYieldBufferGenerator();
        _simple = new SimpleGroundUpVariationGenerator();
        _streaming = new StreamingVariationGenerator();
    }

    [Benchmark(Baseline = true)] // baseline comparison
    public List<string> SimpleConverter(){
        return _input.Select(word => _simple.Generate(word)).SelectMany(v => v).ToList();
    }

    [Benchmark]
    public List<string> StreamingConverter()
    {
        return _input.Select(word => _streaming.Generate(word)).SelectMany(v => v).ToList();
    }

    [Benchmark]
    public List<string> BranchingConverter()
    {
        return _input.Select(word => _branching.Generate(word)).SelectMany(v => v).ToList();
    }
    [Benchmark]
    public List<string> BranchingBufferConverter()
    {
        return _input.Select(word => _branching.Generate(word)).SelectMany(v => v).ToList();
    }
    [Benchmark]
    public List<string> BranchingYieldConverter()
    {
        return _input.Select(word => _branching.Generate(word)).SelectMany(v => v).ToList();
    }

    [Benchmark]
    public List<string> BranchingYieldBufferConverter()
    {
        return _input.Select(word => _branchingYieldBuffer.Generate(word)).SelectMany(v => v).ToList();
    }
    [Benchmark]
    public List<string> BitMaskConverter()
    {
        return _input.Select(word => _bitmask.Generate(word)).SelectMany(v => v).ToList();
    }
    [Benchmark]
    public List<string> BitMaskBufferConverter()
    {
        return _input.Select(word => _bitmask.Generate(word)).SelectMany(v => v).ToList();
    }
    [Benchmark]
    public List<string> BitMaskYieldConverter()
    {
        return _input.Select(word => _bitmask.Generate(word)).SelectMany(v => v).ToList();
    }
    [Benchmark]
    public List<string> BitMaskYieldBufferConverter()
    {
        return _input.Select(word => _bitmask.Generate(word)).SelectMany(v => v).ToList();
    }

}