using System.Buffers;
using System.Text;
using FileMerger.Configurations;
using FileMerger.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileMerger.Services;

public sealed class CsvFileWriter(
    ILogger<CsvFileWriter> logger,
    IOptions<MergeOptions> options)
    : IFileWriter
{
    private static readonly char[] QuoteChars = [',', '"', '\n', '\r'];

    public async Task WriteAsync(
        string outputPath,
        string[] sortedIds,
        string[] mergedHeaders,
        ParsedFileData[] parsedFiles,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Writing {RowCount} rows to {OutputPath}", sortedIds.Length, outputPath);

        // Ensure output directory exists
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            logger.LogDebug("Created output directory {Directory}", directory);
        }

        await using FileStream stream = new(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            options.Value.BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using StreamWriter writer = new(stream, Encoding.UTF8, options.Value.BufferSize);

        await WriteHeaderAsync(writer, mergedHeaders, cancellationToken);
        int[] columnCounts = parsedFiles.Select(f => f.Headers.Length).ToArray();

        StringBuilder rowBuilder = new(512);
        int processedCount = 0;
        int lastProgressReport = 0;

        foreach (string id in sortedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            rowBuilder.Clear();
            AppendField(rowBuilder, id, isFirst: true);

            for (int fileIndex = 0; fileIndex < parsedFiles.Length; fileIndex++)
            {
                ParsedFileData file = parsedFiles[fileIndex];

                if (file.RecordsById.TryGetValue(id, out string[]? fields))
                {
                    foreach (string field in fields)
                    {
                        AppendField(rowBuilder, field, isFirst: false);
                    }
                }
                else
                {
                    // Fill with empty values for missing records
                    for (int i = 0; i < columnCounts[fileIndex]; i++)
                    {
                        rowBuilder.Append(',');
                    }
                }
            }

            // Use ArrayPool to avoid ToString() allocation per row
            char[] buffer = ArrayPool<char>.Shared.Rent(rowBuilder.Length);
            try
            {
                rowBuilder.CopyTo(0, buffer, 0, rowBuilder.Length);
                await writer.WriteAsync(buffer.AsMemory(0, rowBuilder.Length), cancellationToken);
                await writer.WriteLineAsync();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }

            processedCount++;
            if (processedCount - lastProgressReport >= options.Value.ProgressReportInterval)
            {
                double percentage = (double)processedCount / sortedIds.Length * 100;
                logger.LogInformation("Progress: {Processed:N0}/{Total:N0} rows ({Percentage:F1}%)",
                    processedCount, sortedIds.Length, percentage);
                lastProgressReport = processedCount;
            }
        }

        await writer.FlushAsync(cancellationToken);
        logger.LogInformation("Completed writing {RowCount:N0} rows", processedCount);
    }

    private async Task WriteHeaderAsync(StreamWriter writer, string[] headers, CancellationToken cancellationToken)
    {
        StringBuilder headerBuilder = new(256);
        headerBuilder.Append("ID");

        foreach (string header in headers)
        {
            AppendField(headerBuilder, header, isFirst: false);
        }

        // Use ArrayPool to avoid ToString() allocation
        char[] buffer = ArrayPool<char>.Shared.Rent(headerBuilder.Length);
        try
        {
            headerBuilder.CopyTo(0, buffer, 0, headerBuilder.Length);
            await writer.WriteAsync(buffer.AsMemory(0, headerBuilder.Length), cancellationToken);
            await writer.WriteLineAsync();
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static void AppendField(StringBuilder builder, string value, bool isFirst)
    {
        if (!isFirst)
        {
            builder.Append(',');
        }

        // Use IndexOfAny for single-pass check instead of 4 separate Contains calls
        bool needsQuoting = value.AsSpan().IndexOfAny(QuoteChars) >= 0;

        if (needsQuoting)
        {
            builder.Append('"');
            foreach (char c in value)
            {
                if (c == '"')
                {
                    // Append two chars instead of string literal to avoid allocation
                    builder.Append('"');
                    builder.Append('"');
                }
                else
                {
                    builder.Append(c);
                }
            }
            builder.Append('"');
        }
        else
        {
            builder.Append(value);
        }
    }
}
