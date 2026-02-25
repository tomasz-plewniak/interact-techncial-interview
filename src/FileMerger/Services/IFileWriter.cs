using FileMerger.Models;

namespace FileMerger.Services;

public interface IFileWriter
{
    Task WriteAsync(
        string outputPath,
        string[] sortedIds,
        string[] mergedHeaders,
        ParsedFileData[] parsedFiles,
        CancellationToken cancellationToken = default);
}
