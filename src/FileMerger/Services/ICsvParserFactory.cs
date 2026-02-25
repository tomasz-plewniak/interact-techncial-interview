namespace FileMerger.Services;

public interface ICsvParserFactory
{
    ICsvParser CreateParser(char delimiter, char quote = '"');
    ICsvParser CreateParserWithDelimiterDetection();
}
