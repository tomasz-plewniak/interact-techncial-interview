namespace FileMerger.Services;

public interface IDataMerger
{
    Task MergeFilesAsync(
        string[] inputFiles,
        string outputPath,
        CancellationToken cancellationToken = default);
}