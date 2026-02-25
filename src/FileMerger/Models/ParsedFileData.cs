namespace FileMerger.Models;

public sealed class ParsedFileData
{
    /// <summary>
    /// Column headers (excluding the ID column).
    /// </summary>
    public required string[] Headers { get; init; }

    /// <summary>
    /// Records indexed by ID. Values are field arrays (excluding ID).
    /// </summary>
    public required Dictionary<string, string[]> RecordsById { get; init; }

    /// <summary>
    /// Original index of the ID column in the source file.
    /// </summary>
    public required int IdColumnIndex { get; init; }

    /// <summary>
    /// Source file name for logging purposes.
    /// </summary>
    public required string SourceFileName { get; init; }
}