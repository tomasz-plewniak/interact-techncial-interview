using FileMerger.Configurations;
using FileMerger.Models;
using FileMerger.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace UnitTests.Services;

public class CsvFileWriterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IFileWriter _fileWriter;
    
    public CsvFileWriterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CsvFileWriterTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        ILogger<CsvFileWriter> logger = Substitute.For<ILogger<CsvFileWriter>>();
        IOptions<MergeOptions> options = Options.Create(new MergeOptions
        {
            BufferSize = 4096,
            ProgressReportInterval = 100000,
            QuoteCharacter = '"'
        });
        
        _fileWriter = new CsvFileWriter(logger, options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
    
    [Fact]
    public async Task WriteAsync_SimpleData_WritesCorrectOutput()
    {
        string outputPath = GetOutputPath("simple.csv");
        string[] ids = ["1", "2"];
        string[] headers = ["Name", "Value"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Alice", "100"],
            ["2"] = ["Bob", "200"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal(3, lines.Length);
        Assert.Equal("ID,Name,Value", lines[0]);
        Assert.Equal("1,Alice,100", lines[1]);
        Assert.Equal("2,Bob,200", lines[2]);
    }

    [Fact]
    public async Task WriteAsync_EmptyData_WritesOnlyHeader()
    {
        string outputPath = GetOutputPath("empty.csv");
        string[] ids = [];
        string[] headers = ["Name"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>());

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Single(lines);
        Assert.Equal("ID,Name", lines[0]);
    }

    [Fact]
    public async Task WriteAsync_SingleRecord_WritesCorrectly()
    {
        string outputPath = GetOutputPath("single.csv");
        string[] ids = ["1"];
        string[] headers = ["Name"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal(2, lines.Length);
        Assert.Equal("ID,Name", lines[0]);
        Assert.Equal("1,Alice", lines[1]);
    }
    
    [Fact]
    public async Task WriteAsync_MultipleFiles_MergesColumns()
    {
        string outputPath = GetOutputPath("merged.csv");
        string[] ids = ["1"];
        string[] headers = ["Name", "Age"];
        ParsedFileData file1 = CreateParsedFile(["Name"], new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"]
        });
        ParsedFileData file2 = CreateParsedFile(["Age"], new Dictionary<string, string[]>
        {
            ["1"] = ["30"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file1, file2], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("ID,Name,Age", lines[0]);
        Assert.Equal("1,Alice,30", lines[1]);
    }

    [Fact]
    public async Task WriteAsync_MissingRecordInFile_FillsWithEmpty()
    {
        string outputPath = GetOutputPath("missing.csv");
        string[] ids = ["1", "2"];
        string[] headers = ["Name", "Age"];
        ParsedFileData file1 = CreateParsedFile(["Name"], new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"],
            ["2"] = ["Bob"]
        });
        ParsedFileData file2 = CreateParsedFile(["Age"], new Dictionary<string, string[]>
        {
            ["1"] = ["30"]
            // ID "2" is missing
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file1, file2], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("1,Alice,30", lines[1]);
        Assert.Equal("2,Bob,", lines[2]); // Empty value for missing record
    }

    [Fact]
    public async Task WriteAsync_MultipleColumnsInSecondFile_FillsAllWithEmpty()
    {
        string outputPath = GetOutputPath("multi_missing.csv");
        string[] ids = ["1", "2"];
        string[] headers = ["Name", "Age", "City"];
        ParsedFileData file1 = CreateParsedFile(["Name"], new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"],
            ["2"] = ["Bob"]
        });
        ParsedFileData file2 = CreateParsedFile(["Age", "City"], new Dictionary<string, string[]>
        {
            ["1"] = ["30", "NYC"]
            // ID "2" is missing
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file1, file2], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("2,Bob,,", lines[2]); // Two empty values for missing record
    }
    
    [Fact]
    public async Task WriteAsync_FieldWithComma_QuotesField()
    {
        string outputPath = GetOutputPath("comma.csv");
        string[] ids = ["1"];
        string[] headers = ["Name"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Smith, John"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("1,\"Smith, John\"", lines[1]);
    }

    [Fact]
    public async Task WriteAsync_FieldWithQuote_EscapesQuote()
    {
        string outputPath = GetOutputPath("quote.csv");
        string[] ids = ["1"];
        string[] headers = ["Quote"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["She said \"Hello\""]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("1,\"She said \"\"Hello\"\"\"", lines[1]);
    }

    [Fact]
    public async Task WriteAsync_FieldWithNewline_QuotesField()
    {
        string outputPath = GetOutputPath("newline.csv");
        string[] ids = ["1"];
        string[] headers = ["Text"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Line1\nLine2"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string content = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Contains("\"Line1\nLine2\"", content);
    }

    [Fact]
    public async Task WriteAsync_FieldWithCarriageReturn_QuotesField()
    {
        string outputPath = GetOutputPath("cr.csv");
        string[] ids = ["1"];
        string[] headers = ["Text"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Line1\rLine2"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string content = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Contains("\"Line1\rLine2\"", content);
    }

    [Fact]
    public async Task WriteAsync_HeaderWithComma_QuotesHeader()
    {
        string outputPath = GetOutputPath("header_comma.csv");
        string[] ids = ["1"];
        string[] headers = ["Name, First"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("ID,\"Name, First\"", lines[0]);
    }

    [Fact]
    public async Task WriteAsync_IdWithComma_QuotesId()
    {
        string outputPath = GetOutputPath("id_comma.csv");
        string[] ids = ["ID,1"];
        string[] headers = ["Name"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["ID,1"] = ["Alice"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("\"ID,1\",Alice", lines[1]);
    }
    
    [Fact]
    public async Task WriteAsync_OutputDirectoryNotExists_CreatesDirectory()
    {
        string subDir = Path.Combine(_testDirectory, "subdir", "nested");
        string outputPath = Path.Combine(subDir, "output.csv");
        string[] ids = ["1"];
        string[] headers = ["Name"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        Assert.True(Directory.Exists(subDir));
        Assert.True(File.Exists(outputPath));
    }
    
    [Fact]
    public async Task WriteAsync_CancellationRequested_ThrowsOperationCancelledException()
    {
        string outputPath = GetOutputPath("cancel.csv");
        string[] ids = ["1"];
        string[] headers = ["Name"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"]
        });
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _fileWriter.WriteAsync(outputPath, ids, headers, [file], cts.Token));
    }

    [Fact]
    public async Task WriteAsync_IdsInOrder_MaintainsOrder()
    {
        string outputPath = GetOutputPath("ordered.csv");
        string[] ids = ["3", "1", "2"];
        string[] headers = ["Name"];
        ParsedFileData file = CreateParsedFile(headers, new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"],
            ["2"] = ["Bob"],
            ["3"] = ["Charlie"]
        });

        await _fileWriter.WriteAsync(outputPath, ids, headers, [file], TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Equal("3,Charlie", lines[1]);
        Assert.Equal("1,Alice", lines[2]);
        Assert.Equal("2,Bob", lines[3]);
    }
    
    
    private string GetOutputPath(string fileName) => Path.Combine(_testDirectory, fileName);
    
    private static ParsedFileData CreateParsedFile(
        string[] headers,
        Dictionary<string, string[]> records,
        string sourceFileName = "test.csv")
    {
        return new ParsedFileData
        {
            Headers = headers,
            RecordsById = records,
            IdColumnIndex = 0,
            SourceFileName = sourceFileName
        };
    }
}
