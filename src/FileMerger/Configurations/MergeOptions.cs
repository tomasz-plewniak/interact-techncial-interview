namespace FileMerger.Configurations;

public sealed class MergeOptions
{
    public int BufferSize { get; set; } = 524288;
    public int ProgressReportInterval { get; set; } = 100000;
    public char? Delimiter { get; set; }
    public char QuoteCharacter { get; set; } = '"';
}