using System.Diagnostics;
using FileMerger.Configurations;
using FileMerger.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Build service provider with DI
await using ServiceProvider serviceProvider = BuildServiceProvider();
ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
IDataMerger dataMerger = serviceProvider.GetRequiredService<IDataMerger>();

logger.LogInformation("File Merger - Starting");

if (args.Length < 2)
{
    PrintUsage();
    return 1;
}

string[] inputFiles = args.SkipLast(1).ToArray();
string outputPath = args.Last();

foreach (string file in inputFiles)
{
    if (!File.Exists(file))
    {
        logger.LogError("File {FileName} not found", file);
        return 1;
    }
}

logger.LogInformation("Processing {FileCount} input file(s)", inputFiles.Length);
logger.LogInformation("Output path: {OutputPath}", outputPath);

string? outputDir = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
    logger.LogInformation("Created output directory: {OutputDir}", outputDir);
}

Stopwatch stopwatch = Stopwatch.StartNew();

try
{
    using CancellationTokenSource cts = new();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
        logger.LogWarning("Cancellation requested by user");
    };
    
    await dataMerger.MergeFilesAsync(inputFiles, outputPath, cts.Token);

    stopwatch.Stop();

    FileInfo outputInfo = new(outputPath);
    logger.LogInformation("=== RESULTS ===");
    logger.LogInformation("Total execution time: {Elapsed:F3} seconds ({ElapsedMs} ms)",
        stopwatch.Elapsed.TotalSeconds, stopwatch.ElapsedMilliseconds);
    logger.LogInformation("Output file size: {Size:N0} bytes", outputInfo.Length);

    return 0;    
}
catch (OperationCanceledException)
{
    stopwatch.Stop();
    logger.LogWarning("Operation cancelled after {Elapsed:F3} seconds", stopwatch.Elapsed.TotalSeconds);
    return 2;
}
catch (Exception ex)
{
    stopwatch.Stop();
    logger.LogError(ex, "Merge failed after {Elapsed:F3} seconds", stopwatch.Elapsed.TotalSeconds);
    return 1;
}


static ServiceProvider BuildServiceProvider()
{
    ServiceCollection services = new();

    // Configure logging
    services.AddLogging(builder =>
    {
        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddConsole(options =>
        {
            options.FormatterName = "simple";
        });
    });
    
    // Configure options
    services.Configure<MergeOptions>(options =>
    {
        options.BufferSize = 65536;        // 64KB buffer
        options.ProgressReportInterval = 100000;
        options.QuoteCharacter = '"';
    });
    
    // Register services
    services.AddSingleton<IDataMerger, DataMerger>();
    services.AddSingleton<ICsvParserFactory, CsvParserFactory>();
    services.AddSingleton<IFileReader, FileReader>();
    services.AddSingleton<IFileWriter, CsvFileWriter>();

    return services.BuildServiceProvider();
}

static void PrintUsage()
{
    Console.WriteLine("File Merger - Merge multiple delimited files by ID column");
    Console.WriteLine();
    Console.WriteLine("Usage: FileMerger <input_file1> <input_file2> ... <input_fileN> <output_path>");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  input_files   One or more input files to merge (minimum 1)");
    Console.WriteLine("  output_path   Path for the merged output file");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  FileMerger file1.csv file2.csv file3.csv merged.csv");
    Console.WriteLine();
}