using FileMerger.Services;
using FileMerger.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace UnitTests.Services;

public class DataMergerTests
{
    private readonly IFileReader _fileReader;
    private readonly IFileWriter _fileWriter;
    private readonly IDataMerger _dataMerger;

    public DataMergerTests()
    {
        var logger = Substitute.For<ILogger<DataMerger>>();
        _fileReader = Substitute.For<IFileReader>();
        _fileWriter = Substitute.For<IFileWriter>();
        
        _dataMerger = new DataMerger(logger, _fileReader, _fileWriter);
    }
    
    [Fact]
    public async Task MergeFilesAsync_SingleFile_CallsWriterWithCorrectData()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";
        ParsedFileData parsedFile = CreateParsedFile(
            ["Name"],
            new Dictionary<string, string[]> { ["1"] = ["Alice"] });

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(parsedFile));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Is<string[]>(ids => ids.Length == 1 && ids[0] == "1"),
            Arg.Is<string[]>(headers => headers.Length == 1 && headers[0] == "Name"),
            Arg.Is<ParsedFileData[]>(files => files.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeFilesAsync_MultipleFiles_MergesHeaders()
    {
        string[] inputFiles = ["/path/file1.csv", "/path/file2.csv"];
        string outputPath = "/path/output.csv";

        ParsedFileData file1 = CreateParsedFile(["Name"], new Dictionary<string, string[]> { ["1"] = ["Alice"] });
        ParsedFileData file2 = CreateParsedFile(["Age"], new Dictionary<string, string[]> { ["1"] = ["30"] });

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file1));
        _fileReader.ReadFileAsync(inputFiles[1], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file2));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Any<string[]>(),
            Arg.Is<string[]>(headers => headers.SequenceEqual(new[] { "Name", "Age" })),
            Arg.Any<ParsedFileData[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeFilesAsync_MultipleFiles_CollectsAllUniqueIds()
    {
        string[] inputFiles = ["/path/file1.csv", "/path/file2.csv"];
        string outputPath = "/path/output.csv";

        ParsedFileData file1 = CreateParsedFile(["Name"], new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"],
            ["2"] = ["Bob"]
        });
        ParsedFileData file2 = CreateParsedFile(["Age"], new Dictionary<string, string[]>
        {
            ["2"] = ["25"],
            ["3"] = ["35"]
        });

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file1));
        _fileReader.ReadFileAsync(inputFiles[1], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file2));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Is<string[]>(ids => ids.Length == 3),
            Arg.Any<string[]>(),
            Arg.Any<ParsedFileData[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeFilesAsync_MultipleFiles_SortsIdsAlphabetically()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";

        ParsedFileData file = CreateParsedFile(["Name"], new Dictionary<string, string[]>
        {
            ["c"] = ["Charlie"],
            ["a"] = ["Alice"],
            ["b"] = ["Bob"]
        });

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Is<string[]>(ids => ids.SequenceEqual(new[] { "a", "b", "c" })),
            Arg.Any<string[]>(),
            Arg.Any<ParsedFileData[]>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task MergeFilesAsync_ReadsAllInputFiles()
    {
        string[] inputFiles = ["/path/file1.csv", "/path/file2.csv", "/path/file3.csv"];
        string outputPath = "/path/output.csv";

        foreach (string file in inputFiles)
        {
            _fileReader.ReadFileAsync(file, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(CreateParsedFile(["Col"], new Dictionary<string, string[]>())));
        }

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileReader.Received(3).ReadFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _fileReader.Received(1).ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>());
        await _fileReader.Received(1).ReadFileAsync(inputFiles[1], Arg.Any<CancellationToken>());
        await _fileReader.Received(1).ReadFileAsync(inputFiles[2], Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task MergeFilesAsync_EmptyFiles_WritesEmptyOutput()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";

        ParsedFileData file = CreateParsedFile(["Name"], new Dictionary<string, string[]>());

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Is<string[]>(ids => ids.Length == 0),
            Arg.Any<string[]>(),
            Arg.Any<ParsedFileData[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeFilesAsync_DuplicateIdsAcrossFiles_DeduplicatesIds()
    {
        string[] inputFiles = ["/path/file1.csv", "/path/file2.csv"];
        string outputPath = "/path/output.csv";

        ParsedFileData file1 = CreateParsedFile(["Name"], new Dictionary<string, string[]>
        {
            ["1"] = ["Alice"],
            ["2"] = ["Bob"]
        });
        ParsedFileData file2 = CreateParsedFile(["Age"], new Dictionary<string, string[]>
        {
            ["1"] = ["30"],
            ["2"] = ["25"]
        });

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file1));
        _fileReader.ReadFileAsync(inputFiles[1], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file2));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Is<string[]>(ids => ids.Length == 2),
            Arg.Any<string[]>(),
            Arg.Any<ParsedFileData[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeFilesAsync_ManyHeaders_CombinesAllHeaders()
    {
        string[] inputFiles = ["/path/file1.csv", "/path/file2.csv"];
        string outputPath = "/path/output.csv";

        ParsedFileData file1 = CreateParsedFile(["Col1", "Col2", "Col3"], new Dictionary<string, string[]>
        {
            ["1"] = ["A", "B", "C"]
        });
        ParsedFileData file2 = CreateParsedFile(["Col4", "Col5"], new Dictionary<string, string[]>
        {
            ["1"] = ["D", "E"]
        });

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file1));
        _fileReader.ReadFileAsync(inputFiles[1], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(file2));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Any<string[]>(),
            Arg.Is<string[]>(headers =>
                headers.Length == 5 &&
                headers.AsEnumerable().SequenceEqual(new[] { "Col1", "Col2", "Col3", "Col4", "Col5" })),
            Arg.Any<ParsedFileData[]>(),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task MergeFilesAsync_CancellationDuringRead_PropagatesCancellation()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";
        using CancellationTokenSource cts = new();

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns<Task<ParsedFileData>>(async _ =>
            {
                await cts.CancelAsync();
                throw new OperationCanceledException();
            });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _dataMerger.MergeFilesAsync(inputFiles, outputPath, cts.Token));
    }

    [Fact]
    public async Task MergeFilesAsync_PassesCancellationTokenToReader()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";
        using CancellationTokenSource cts = new();

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateParsedFile(["Col"], new Dictionary<string, string[]>())));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, cts.Token);

        await _fileReader.Received(1).ReadFileAsync(inputFiles[0], cts.Token);
    }

    [Fact]
    public async Task MergeFilesAsync_PassesCancellationTokenToWriter()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";
        using CancellationTokenSource cts = new();

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateParsedFile(["Col"], new Dictionary<string, string[]>())));

        await _dataMerger.MergeFilesAsync(inputFiles, outputPath, cts.Token);

        await _fileWriter.Received(1).WriteAsync(
            outputPath,
            Arg.Any<string[]>(),
            Arg.Any<string[]>(),
            Arg.Any<ParsedFileData[]>(),
            cts.Token);
    }
    
    [Fact]
    public async Task MergeFilesAsync_ReaderThrowsException_PropagatesException()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns<Task<ParsedFileData>>(_ => throw new InvalidDataException("Test error"));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MergeFilesAsync_WriterThrowsException_PropagatesException()
    {
        string[] inputFiles = ["/path/file1.csv"];
        string outputPath = "/path/output.csv";

        _fileReader.ReadFileAsync(inputFiles[0], Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateParsedFile(["Col"], new Dictionary<string, string[]>())));
        _fileWriter.WriteAsync(Arg.Any<string>(), Arg.Any<string[]>(), Arg.Any<string[]>(),
                Arg.Any<ParsedFileData[]>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new IOException("Disk full"));

        await Assert.ThrowsAsync<IOException>(
            () => _dataMerger.MergeFilesAsync(inputFiles, outputPath, TestContext.Current.CancellationToken));
    }
    
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
