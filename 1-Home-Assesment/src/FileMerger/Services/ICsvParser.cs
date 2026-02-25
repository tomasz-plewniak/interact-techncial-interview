namespace FileMerger.Services;

public interface ICsvParser
{
    string[] ParseLine(ReadOnlySpan<char> line);
    string FormatField(string value);
    char DetectDelimiter(string headerLine);
}