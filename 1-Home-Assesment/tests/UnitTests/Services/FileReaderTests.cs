using FileMerger.Configurations;
using FileMerger.Models;
using FileMerger.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Services;

public class FileReaderTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILogger<FileReader> _logger;
    private readonly IOptions<MergeOptions> _options;
    private readonly ICsvParserFactory _parserFactory;
    private readonly IFileReader _fileReader;

    public FileReaderTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileReaderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _logger = Substitute.For<ILogger<FileReader>>();
        _options = Options.Create(new MergeOptions
        {
            BufferSize = 4096,
            QuoteCharacter = '"'
        });
        _parserFactory = new CsvParserFactory(_options);

        _fileReader = new FileReader(_logger, _options, _parserFactory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
    
    [Fact]
    public async Task ReadFileAsync_SimpleFile_ReturnsCorrectData()
    {
        string filePath = CreateTestFile("simple.csv", "ID,Name,Value\n1,Alice,100\n2,Bob,200");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Name", "Value"], result.Headers);
        Assert.Equal(2, result.RecordsById.Count);
        Assert.Equal(["Alice", "100"], result.RecordsById["1"]);
        Assert.Equal(["Bob", "200"], result.RecordsById["2"]);
    }

    [Fact]
    public async Task ReadFileAsync_IdColumnInMiddle_ExtractsCorrectly()
    {
        string filePath = CreateTestFile("id_middle.csv", "Name,ID,Value\nAlice,1,100\nBob,2,200");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Name", "Value"], result.Headers);
        Assert.Equal(1, result.IdColumnIndex);
        Assert.Equal(["Alice", "100"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_IdColumnAtEnd_ExtractsCorrectly()
    {
        string filePath = CreateTestFile("id_end.csv", "Name,Value,ID\nAlice,100,1\nBob,200,2");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Name", "Value"], result.Headers);
        Assert.Equal(2, result.IdColumnIndex);
        Assert.Equal(["Alice", "100"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_CaseInsensitiveIdColumn_FindsIdColumn()
    {
        string filePath = CreateTestFile("case_insensitive.csv", "id,Name,Value\n1,Alice,100");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Single(result.RecordsById);
        Assert.Equal(["Alice", "100"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_UppercaseId_FindsIdColumn()
    {
        string filePath = CreateTestFile("uppercase.csv", "ID,Name\n1,Alice");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Single(result.RecordsById);
    }
    
    [Fact]
    public async Task ReadFileAsync_QuotedFieldsWithCommas_ParsesCorrectly()
    {
        string filePath = CreateTestFile("quoted.csv", "ID,Name,Description\n1,\"Smith, John\",\"A, B, C\"");
    
        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Smith, John", "A, B, C"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_QuotedFieldsWithQuotes_ParsesCorrectly()
    {
        string filePath = CreateTestFile("escaped_quotes.csv", "ID,Name,Quote\n1,Alice,\"She said \"\"Hello\"\"\"");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Alice", "She said \"Hello\""], result.RecordsById["1"]);
    }
    
    [Fact]
    public async Task ReadFileAsync_TabDelimited_DetectsDelimiter()
    {
        string filePath = CreateTestFile("tab.tsv", "ID\tName\tValue\n1\tAlice\t100");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Name", "Value"], result.Headers);
        Assert.Equal(["Alice", "100"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_PipeDelimited_DetectsDelimiter()
    {
        string filePath = CreateTestFile("pipe.csv", "ID|Name|Value\n1|Alice|100");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Name", "Value"], result.Headers);
        Assert.Equal(["Alice", "100"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_SemicolonDelimited_DetectsDelimiter()
    {
        string filePath = CreateTestFile("semicolon.csv", "ID;Name;Value\n1;Alice;100");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Name", "Value"], result.Headers);
        Assert.Equal(["Alice", "100"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_ExplicitDelimiter_UsesSpecifiedDelimiter()
    {
        IOptions<MergeOptions> options = Options.Create(new MergeOptions
        {
            BufferSize = 4096,
            Delimiter = '|',
            QuoteCharacter = '"'
        });
        ICsvParserFactory parserFactory = new CsvParserFactory(options);
        FileReader reader = new(_logger, options, parserFactory);
        string filePath = CreateTestFile("explicit.csv", "ID|Name|Value\n1|Alice|100");

        ParsedFileData result = await reader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(["Name", "Value"], result.Headers);
    }
    
    [Fact]
    public async Task ReadFileAsync_NoIdColumn_ThrowsInvalidDataException()
    {
        string filePath = CreateTestFile("no_id.csv", "Name,Value\nAlice,100");

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken));

        Assert.Contains("ID", exception.Message);
    }

    [Fact]
    public async Task ReadFileAsync_EmptyFile_ThrowsInvalidDataException()
    {
        string filePath = CreateTestFile("empty.csv", "");

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken));

        Assert.Contains("empty", exception.Message);
    }

    [Fact]
    public async Task ReadFileAsync_OnlyWhitespace_ThrowsInvalidDataException()
    {
        string filePath = CreateTestFile("whitespace.csv", "   \n  \n   ");

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken));

        Assert.Contains("empty", exception.Message);
    }

    [Fact]
    public async Task ReadFileAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _fileReader.ReadFileAsync("/nonexistent/file.csv", TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task ReadFileAsync_MismatchedFieldCount_SkipsLine()
    {
        string filePath = CreateTestFile("mismatched.csv", "ID,Name,Value\n1,Alice,100\n2,Bob\n3,Charlie,300");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.RecordsById.Count);
        Assert.True(result.RecordsById.ContainsKey("1"));
        Assert.True(result.RecordsById.ContainsKey("3"));
        Assert.False(result.RecordsById.ContainsKey("2"));
    }

    [Fact]
    public async Task ReadFileAsync_EmptyId_SkipsLine()
    {
        string filePath = CreateTestFile("empty_id.csv", "ID,Name,Value\n1,Alice,100\n,Bob,200\n3,Charlie,300");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.RecordsById.Count);
        Assert.False(result.RecordsById.ContainsKey(""));
    }

    [Fact]
    public async Task ReadFileAsync_DuplicateIds_LastValueWins()
    {
        string filePath = CreateTestFile("duplicate.csv", "ID,Name\n1,Alice\n1,Updated");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Single(result.RecordsById);
        Assert.Equal(["Updated"], result.RecordsById["1"]);
    }

    [Fact]
    public async Task ReadFileAsync_BlankLines_SkipsBlankLines()
    {
        string filePath = CreateTestFile("blank_lines.csv", "ID,Name\n\n1,Alice\n\n2,Bob\n");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.RecordsById.Count);
    }
    
    [Fact]
    public async Task ReadFileAsync_CancellationRequested_ThrowsOperationCancelledException()
    {
        string filePath = CreateTestFile("cancel.csv", "ID,Name\n1,Alice");
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _fileReader.ReadFileAsync(filePath, cts.Token));
    }
    
    [Fact]
    public async Task ReadFileAsync_ReturnsCorrectSourceFileName()
    {
        string filePath = CreateTestFile("metadata.csv", "ID,Name\n1,Alice");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal("metadata.csv", result.SourceFileName);
    }

    [Fact]
    public async Task ReadFileAsync_ReturnsCorrectIdColumnIndex()
    {
        string filePath = CreateTestFile("index.csv", "Name,ID,Value\n1,Alice,100");

        ParsedFileData result = await _fileReader.ReadFileAsync(filePath, TestContext.Current.CancellationToken);

        Assert.Equal(1, result.IdColumnIndex);
    }
    
    private string CreateTestFile(string fileName, string content)
    {
        string filePath = Path.Combine(_testDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
