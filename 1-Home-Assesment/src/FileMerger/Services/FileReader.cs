using System.Text;
using FileMerger.Configurations;
using FileMerger.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileMerger.Services;

public class FileReader(
    ILogger<FileReader> logger,
    IOptions<MergeOptions> options,
    ICsvParserFactory parserFactory) : IFileReader
{
public async Task<ParsedFileData> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string fileName = Path.GetFileName(filePath);
        FileInfo fileInfo = new(filePath);
        // Pre-size dictionary based on estimated record count (~50 bytes per record average)
        int estimatedRecords = (int)(fileInfo.Length / 50);
        Dictionary<string, string[]> records = new(estimatedRecords, StringComparer.Ordinal);
        string[]? headers = null;
        int idColumnIndex = -1;
        ICsvParser? parser = null;

        logger.LogInformation("Reading {FileName} ({Size:N0} bytes)", fileName, fileInfo.Length);

        await using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            options.Value.BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, options.Value.BufferSize);

        int lineNumber = 0;
        int skippedLines = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // First non-empty line is the header
            if (headers == null)
            {
                char delimiter = options.Value.Delimiter ?? parserFactory.CreateParserWithDelimiterDetection().DetectDelimiter(line);
                parser = parserFactory.CreateParser(delimiter, options.Value.QuoteCharacter);
                headers = parser.ParseLine(line.AsSpan());

                // Find ID column (case-insensitive)
                idColumnIndex = Array.FindIndex(headers, h =>
                    h.Equals("ID", StringComparison.OrdinalIgnoreCase));

                if (idColumnIndex == -1)
                {
                    logger.LogError("File {FileName} does not contain an 'ID' column", fileName);
                    throw new InvalidDataException($"File {filePath} does not contain an 'ID' column.");
                }

                logger.LogDebug("Detected {ColumnCount} columns in {FileName}, ID at index {IdIndex}",
                    headers.Length, fileName, idColumnIndex);
                continue;
            }

            string[] fields = parser!.ParseLine(line.AsSpan());

            // Validate field count
            if (fields.Length != headers.Length)
            {
                logger.LogWarning(
                    "Line {LineNumber} in {FileName} has {ActualFields} fields, expected {ExpectedFields}. Skipping",
                    lineNumber, fileName, fields.Length, headers.Length);
                skippedLines++;
                continue;
            }

            string id = fields[idColumnIndex];
            if (string.IsNullOrEmpty(id))
            {
                logger.LogWarning("Line {LineNumber} in {FileName} has empty ID. Skipping", lineNumber, fileName);
                skippedLines++;
                continue;
            }

            // Store non-ID columns for memory efficiency
            string[] nonIdFields = ExtractNonIdFields(fields, idColumnIndex);
            records[id] = nonIdFields;
        }

        if (headers == null)
        {
            logger.LogError("File {FileName} is empty or contains only whitespace", fileName);
            throw new InvalidDataException($"File {filePath} is empty or contains only whitespace.");
        }

        string[] nonIdHeaders = ExtractNonIdFields(headers, idColumnIndex);

        if (skippedLines > 0)
        {
            logger.LogWarning("Skipped {SkippedLines} malformed lines in {FileName}", skippedLines, fileName);
        }

        logger.LogInformation("Parsed {RecordCount} records with {ColumnCount} columns from {FileName}",
            records.Count, nonIdHeaders.Length, fileName);

        return new ParsedFileData
        {
            Headers = nonIdHeaders,
            RecordsById = records,
            IdColumnIndex = idColumnIndex,
            SourceFileName = fileName
        };
    }

    private static string[] ExtractNonIdFields(string[] fields, int idColumnIndex)
    {
        string[] result = new string[fields.Length - 1];
        int targetIndex = 0;

        for (int i = 0; i < fields.Length; i++)
        {
            if (i != idColumnIndex)
            {
                result[targetIndex++] = fields[i];
            }
        }

        return result;
    }
}