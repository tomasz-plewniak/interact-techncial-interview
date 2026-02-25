using System.Text;
using FileMerger.Configurations;
using Microsoft.Extensions.Options;

namespace FileMerger.Services;

/// <summary>
/// Custom CSV parser that handles quoted fields, escaped quotes, and embedded delimiters.
/// Implements RFC 4180 compliant parsing without using third-party libraries.
/// </summary>
public sealed class CsvParser : ICsvParser
{
    private readonly char _delimiter;
    private readonly char _quote;
    private readonly char[] _quoteChars;
    private static readonly char[] PossibleDelimiters = [',', '\t', '|', ';'];

    public CsvParser(IOptions<MergeOptions> options)
    {
        MergeOptions config = options.Value;
        _delimiter = config.Delimiter ?? ',';
        _quote = config.QuoteCharacter;
        _quoteChars = [_delimiter, _quote, '\n', '\r'];
    }

    /// <summary>
    /// Creates a parser instance with a specific delimiter.
    /// Used when delimiter is detected from file content.
    /// </summary>
    public CsvParser(char delimiter, char quote = '"')
    {
        _delimiter = delimiter;
        _quote = quote;
        _quoteChars = [delimiter, quote, '\n', '\r'];
    }

    /// <inheritdoc />
    public string[] ParseLine(ReadOnlySpan<char> line)
    {
        // Count fields first to pre-allocate exact array size (single pass)
        int fieldCount = 1; // At least one field
        bool countingInQuotes = false;
        for (int j = 0; j < line.Length; j++)
        {
            char ch = line[j];
            if (ch == _quote)
            {
                countingInQuotes = !countingInQuotes;
            }
            else if (ch == _delimiter && !countingInQuotes)
            {
                fieldCount++;
            }
        }

        // Pre-allocate array with exact size
        string[] fields = new string[fieldCount];
        int fieldIndex = 0;
        StringBuilder currentField = new(64);
        bool inQuotes = false;
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == _quote)
                {
                    // Check for escaped quote (two consecutive quotes)
                    if (i + 1 < line.Length && line[i + 1] == _quote)
                    {
                        currentField.Append(_quote);
                        i += 2;
                        continue;
                    }
                    // End of quoted field
                    inQuotes = false;
                    i++;
                    continue;
                }
                currentField.Append(c);
            }
            else
            {
                if (c == _quote)
                {
                    // Start of quoted field
                    inQuotes = true;
                }
                else if (c == _delimiter)
                {
                    // End of field - store directly in array
                    fields[fieldIndex++] = currentField.ToString();
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            i++;
        }

        // Add the last field
        fields[fieldIndex] = currentField.ToString();

        return fields;
    }

    /// <inheritdoc />
    public string FormatField(string value)
    {
        // Use IndexOfAny for single-pass check instead of 4 separate Contains calls
        bool needsQuoting = value.AsSpan().IndexOfAny(_quoteChars) >= 0;

        if (!needsQuoting)
        {
            return value;
        }

        StringBuilder builder = new(value.Length + 10);
        builder.Append(_quote);

        foreach (char c in value)
        {
            if (c == _quote)
            {
                builder.Append(_quote); // Escape quote with double quote
            }
            builder.Append(c);
        }

        builder.Append(_quote);
        return builder.ToString();
    }

    /// <inheritdoc />
    public char DetectDelimiter(string headerLine)
    {
        // Single-pass counting of all possible delimiters
        Span<int> counts = stackalloc int[PossibleDelimiters.Length];
        bool inQuotes = false;

        foreach (char c in headerLine)
        {
            if (c == _quote)
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes)
            {
                // Check against all delimiters in one iteration
                for (int i = 0; i < PossibleDelimiters.Length; i++)
                {
                    if (c == PossibleDelimiters[i])
                    {
                        counts[i]++;
                        break;
                    }
                }
            }
        }

        // Find delimiter with max count
        int maxCount = 0;
        char detectedDelimiter = ',';

        for (int i = 0; i < PossibleDelimiters.Length; i++)
        {
            if (counts[i] > maxCount)
            {
                maxCount = counts[i];
                detectedDelimiter = PossibleDelimiters[i];
            }
        }

        return detectedDelimiter;
    }
}
