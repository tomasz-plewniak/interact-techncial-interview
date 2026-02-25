using FileMerger.Models;

namespace FileMerger.Services;

public interface IFileReader
{
    Task<ParsedFileData> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
}