using FileMerger.Configurations;
using FileMerger.Services;
using Microsoft.Extensions.Options;

namespace UnitTests.Services;

public class CsvParserFactoryTests
{
    private readonly IOptions<MergeOptions> _options;
    private readonly ICsvParserFactory _factory;

    public CsvParserFactoryTests()
    {
        _options = Options.Create(new MergeOptions
        {
            Delimiter = ',',
            QuoteCharacter = '"'
        });
        _factory = new CsvParserFactory(_options);
    }

    [Fact]
    public void CreateParser_WithCustomDelimiter_ReturnsParserWithCorrectDelimiter()
    {
        ICsvParser parser = _factory.CreateParser('|', '"');

        string[] result = parser.ParseLine("value1|value2|value3");

        Assert.Equal(["value1", "value2", "value3"], result);
    }

    [Fact]
    public void CreateParser_WithCustomQuote_ReturnsParserWithCorrectQuote()
    {
        ICsvParser parser = _factory.CreateParser(',', '\'');

        string[] result = parser.ParseLine("value1,'quoted,value',value3");

        Assert.Equal(["value1", "quoted,value", "value3"], result);
    }

    [Fact]
    public void CreateParser_WithDefaultQuote_UsesDoubleQuote()
    {
        ICsvParser parser = _factory.CreateParser(',');

        string[] result = parser.ParseLine("value1,\"quoted,value\",value3");

        Assert.Equal(["value1", "quoted,value", "value3"], result);
    }

    [Fact]
    public void CreateParserWithDelimiterDetection_ReturnsParserUsingOptionsDelimiter()
    {
        IOptions<MergeOptions> options = Options.Create(new MergeOptions
        {
            Delimiter = '|',
            QuoteCharacter = '"'
        });
        ICsvParserFactory factory = new CsvParserFactory(options);

        ICsvParser parser = factory.CreateParserWithDelimiterDetection();
        string[] result = parser.ParseLine("value1|value2|value3");

        Assert.Equal(["value1", "value2", "value3"], result);
    }

    [Fact]
    public void CreateParserWithDelimiterDetection_CanDetectDelimiter()
    {
        ICsvParser parser = _factory.CreateParserWithDelimiterDetection();

        char detectedDelimiter = parser.DetectDelimiter("header1|header2|header3");

        Assert.Equal('|', detectedDelimiter);
    }

    [Fact]
    public void CreateParserWithDelimiterDetection_DetectsTabDelimiter()
    {
        ICsvParser parser = _factory.CreateParserWithDelimiterDetection();

        char detectedDelimiter = parser.DetectDelimiter("header1\theader2\theader3");

        Assert.Equal('\t', detectedDelimiter);
    }

    [Fact]
    public void CreateParserWithDelimiterDetection_DetectsSemicolonDelimiter()
    {
        ICsvParser parser = _factory.CreateParserWithDelimiterDetection();

        char detectedDelimiter = parser.DetectDelimiter("header1;header2;header3");

        Assert.Equal(';', detectedDelimiter);
    }

    [Fact]
    public void CreateParserWithDelimiterDetection_DefaultsToComma()
    {
        ICsvParser parser = _factory.CreateParserWithDelimiterDetection();

        char detectedDelimiter = parser.DetectDelimiter("headeronly");

        Assert.Equal(',', detectedDelimiter);
    }

    [Fact]
    public void CreateParser_MultipleInstances_AreIndependent()
    {
        ICsvParser parser1 = _factory.CreateParser(',');
        ICsvParser parser2 = _factory.CreateParser('|');

        string[] result1 = parser1.ParseLine("a,b,c");
        string[] result2 = parser2.ParseLine("x|y|z");

        Assert.Equal(["a", "b", "c"], result1);
        Assert.Equal(["x", "y", "z"], result2);
    }
}
