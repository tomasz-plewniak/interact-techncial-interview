using FileMerger.Configurations;
using Microsoft.Extensions.Options;

namespace FileMerger.Services;

public class CsvParserFactory(IOptions<MergeOptions> options) : ICsvParserFactory
{
    public ICsvParser CreateParser(char delimiter, char quote = '"')
    {
        return new CsvParser(delimiter, quote);
    }

    public ICsvParser CreateParserWithDelimiterDetection()
    {
        return new CsvParser(options);
    }
}
