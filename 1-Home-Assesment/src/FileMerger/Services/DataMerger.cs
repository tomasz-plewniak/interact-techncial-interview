using System.Diagnostics;
using FileMerger.Models;
using Microsoft.Extensions.Logging;

namespace FileMerger.Services;

public sealed class DataMerger(
    ILogger<DataMerger> logger,
    IFileReader fileReader,
    IFileWriter fileWriter)
    : IDataMerger
{
    public async Task MergeFilesAsync(
        string[] inputFiles,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting merge of {FileCount} files", inputFiles.Length);

        ParsedFileData[] parsedFiles = await ReadFilesInParallelAsync(inputFiles, cancellationToken);

        string[] sortedIds = CollectAndSortUniqueIds(parsedFiles);

        string[] mergedHeaders = BuildMergedHeaders(parsedFiles);
        logger.LogInformation("Merged schema: {ColumnCount} columns (ID + {PropertyCount} properties)",
            mergedHeaders.Length + 1, mergedHeaders.Length);

        await fileWriter.WriteAsync(outputPath, sortedIds, mergedHeaders, parsedFiles, cancellationToken);

        logger.LogInformation("Merge completed successfully");
    }

    private async Task<ParsedFileData[]> ReadFilesInParallelAsync(
        string[] inputFiles,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Phase 1: Reading {FileCount} files in parallel", inputFiles.Length);
        Stopwatch stopwatch = Stopwatch.StartNew();

        Task<ParsedFileData>[] readTasks = inputFiles.Select(
            async file => await fileReader.ReadFileAsync(file, cancellationToken)).ToArray();

        ParsedFileData[] parsedFiles = await Task.WhenAll(readTasks);

        stopwatch.Stop();
        logger.LogInformation("Reading completed in {Elapsed:F3}s", stopwatch.Elapsed.TotalSeconds);

        return parsedFiles;
    }

    private string[] CollectAndSortUniqueIds(ParsedFileData[] parsedFiles)
    {
        logger.LogInformation("Phase 2: Collecting unique IDs");
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Estimate total IDs for pre-sizing
        int estimatedCount = 0;
        foreach (var file in parsedFiles)
        {
            estimatedCount += file.RecordsById.Count;
        }

        // Use HashSet for simpler semantics - sequential is faster for small file counts
        HashSet<string> allIds = new(estimatedCount, StringComparer.Ordinal);
        foreach (var file in parsedFiles)
        {
            foreach (string id in file.RecordsById.Keys)
            {
                allIds.Add(id);
            }
        }

        string[] sortedIds = allIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();

        stopwatch.Stop();
        logger.LogInformation("Found {IdCount:N0} unique IDs in {Elapsed:F3}s",
            sortedIds.Length, stopwatch.Elapsed.TotalSeconds);

        return sortedIds;
    }

    private static string[] BuildMergedHeaders(ParsedFileData[] parsedFiles)
    {
        // Calculate total columns without LINQ allocation
        int totalColumns = 0;
        foreach (var file in parsedFiles)
        {
            totalColumns += file.Headers.Length;
        }

        // Direct array allocation and copy
        string[] headers = new string[totalColumns];
        int offset = 0;
        foreach (var file in parsedFiles)
        {
            file.Headers.CopyTo(headers, offset);
            offset += file.Headers.Length;
        }

        return headers;
    }
}
