using System.Linq;
using Samwin.UmlautConverterLib.Step1;

namespace Samwin.UmlautConverterLib.Tests.Step1;

public class UmlautEfficientConverterTests
{
    private readonly UmlautEfficientConverter _converter = new();

    [Fact]
    public void Convert_ShouldReturnEmptyString_WhenInputIsEmpty()
    {
        var result = _converter.Convert("");
        Assert.Equal("", result);
    }

    [Fact]
    public void Convert_ShouldReturnNull_WhenInputIsNull()
    {
        string? input = null;
        var result = _converter.Convert(input!); // depending on your null policy
        Assert.Null(result);
    }

    [Fact]
    public void Convert_ShouldReplaceSingleUmlaut()
    {
        var result = _converter.Convert("Mueller");
        Assert.Equal("Müller", result);
    }

    [Fact]
    public void Convert_ShouldReplaceMultipleUmlauts()
    {
        var result = _converter.Convert("Mueller und Schroeder");
        Assert.Equal("Müller und Schröder", result);
    }

    [Fact]
    public void Convert_ShouldNotChangeStringWithoutUmlauts()
    {
        var result = _converter.Convert("Hello World");
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Convert_ShouldHandleConsecutivePatterns()
    {
        var result = _converter.Convert("aessss");
        Assert.Equal("äßß", result);
    }

    [Fact]
    public void Convert_ShouldHandleUpperAndLowerCase()
    {
        var result = _converter.Convert("Aesch Oesterreich Ueber");
        Assert.Equal("Äsch Österreich Über", result);
    }

    [Fact]
    public void Convert_ShouldHandleLongInputEfficiently()
    {
        var input = string.Concat(Enumerable.Repeat("Mueller Schroeder ", 1000));
        var expected = string.Concat(Enumerable.Repeat("Müller Schröder ", 1000));

        var result = _converter.Convert(input);

        Assert.Equal(expected, result);
    }
}