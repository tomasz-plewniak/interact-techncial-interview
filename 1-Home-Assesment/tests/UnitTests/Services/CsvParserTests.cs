using FileMerger.Configurations;
using FileMerger.Services;
using Microsoft.Extensions.Options;

namespace UnitTests.Services;

public class CsvParserTests
{
    [Fact]
    public void ParseLine_SimpleCommaSeparated_ReturnsCorrectFields()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("a,b,c");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void ParseLine_EmptyFields_ReturnsEmptyStrings()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("a,,c");

        Assert.Equal(["a", "", "c"], result);
    }

    [Fact]
    public void ParseLine_QuotedField_RemovesQuotes()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("a,\"b\",c");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void ParseLine_QuotedFieldWithComma_KeepsCommaInField()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("a,\"b,c\",d");

        Assert.Equal(["a", "b,c", "d"], result);
    }

    [Fact]
    public void ParseLine_EscapedQuote_HandlesDoubleQuote()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("a,\"b\"\"c\",d");

        Assert.Equal(["a", "b\"c", "d"], result);
    }

    [Fact]
    public void ParseLine_TabDelimited_ParsesCorrectly()
    {
        CsvParser parser = CreateParser('\t');

        string[] result = parser.ParseLine("a\tb\tc");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void ParseLine_PipeDelimited_ParsesCorrectly()
    {
        CsvParser parser = CreateParser('|');

        string[] result = parser.ParseLine("a|b|c");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void ParseLine_SingleField_ReturnsSingleElement()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("single");

        Assert.Equal(["single"], result);
    }

    [Fact]
    public void ParseLine_EmptyLine_ReturnsSingleEmptyString()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("");

        Assert.Equal([""], result);
    }

    [Fact]
    public void ParseLine_QuotedFieldWithNewline_KeepsNewline()
    {
        CsvParser parser = CreateParser();

        string[] result = parser.ParseLine("a,\"b\nc\",d");

        Assert.Equal(["a", "b\nc", "d"], result);
    }

    [Fact]
    public void FormatField_SimpleValue_ReturnsUnquoted()
    {
        CsvParser parser = CreateParser();

        string result = parser.FormatField("simple");

        Assert.Equal("simple", result);
    }

    [Fact]
    public void FormatField_ValueWithComma_ReturnsQuoted()
    {
        CsvParser parser = CreateParser();

        string result = parser.FormatField("a,b");

        Assert.Equal("\"a,b\"", result);
    }

    [Fact]
    public void FormatField_ValueWithQuote_EscapesAndQuotes()
    {
        CsvParser parser = CreateParser();

        string result = parser.FormatField("a\"b");

        Assert.Equal("\"a\"\"b\"", result);
    }

    [Fact]
    public void FormatField_ValueWithNewline_ReturnsQuoted()
    {
        CsvParser parser = CreateParser();

        string result = parser.FormatField("a\nb");

        Assert.Equal("\"a\nb\"", result);
    }

    [Fact]
    public void FormatField_EmptyString_ReturnsEmpty()
    {
        CsvParser parser = CreateParser();

        string result = parser.FormatField("");

        Assert.Equal("", result);
    }

    [Fact]
    public void DetectDelimiter_CommaSeparated_ReturnsComma()
    {
        CsvParser parser = CreateParser();

        char result = parser.DetectDelimiter("ID,Name,Value");

        Assert.Equal(',', result);
    }

    [Fact]
    public void DetectDelimiter_TabSeparated_ReturnsTab()
    {
        CsvParser parser = CreateParser();

        char result = parser.DetectDelimiter("ID\tName\tValue");

        Assert.Equal('\t', result);
    }

    [Fact]
    public void DetectDelimiter_PipeSeparated_ReturnsPipe()
    {
        CsvParser parser = CreateParser();

        char result = parser.DetectDelimiter("ID|Name|Value");

        Assert.Equal('|', result);
    }

    [Fact]
    public void DetectDelimiter_SemicolonSeparated_ReturnsSemicolon()
    {
        CsvParser parser = CreateParser();

        char result = parser.DetectDelimiter("ID;Name;Value");

        Assert.Equal(';', result);
    }

    [Fact]
    public void DetectDelimiter_NoDelimiter_ReturnsCommaDefault()
    {
        CsvParser parser = CreateParser();

        char result = parser.DetectDelimiter("SingleColumn");

        Assert.Equal(',', result);
    }

    [Fact]
    public void DetectDelimiter_QuotedDelimiter_IgnoresQuotedContent()
    {
        CsvParser parser = CreateParser();

        char result = parser.DetectDelimiter("ID,\"Name,With,Commas\",Value");

        Assert.Equal(',', result);
    }

    [Fact]
    public void Constructor_WithOptions_UsesDefaultDelimiter()
    {
        CsvParser parser = CreateParserWithOptions();

        string[] result = parser.ParseLine("a,b,c");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Constructor_WithCustomDelimiter_UsesSpecifiedDelimiter()
    {
        CsvParser parser = CreateParserWithOptions(delimiter: '|');

        string[] result = parser.ParseLine("a|b|c");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public void Constructor_WithCustomQuote_UsesSpecifiedQuote()
    {
        CsvParser parser = CreateParserWithOptions(quote: '\'');

        string[] result = parser.ParseLine("a,'b,c',d");

        Assert.Equal(["a", "b,c", "d"], result);
    }
    
    private static CsvParser CreateParser(char delimiter = ',', char quote = '"')
    {
        return new CsvParser(delimiter, quote);
    }

    private static CsvParser CreateParserWithOptions(char? delimiter = null, char quote = '"')
    {
        IOptions<MergeOptions> options = Options.Create(new MergeOptions
        {
            Delimiter = delimiter,
            QuoteCharacter = quote
        });
        return new CsvParser(options);
    }
}
