namespace BookScraper.Services.Export;

public enum ExportFormat
{
    Csv,
    Json,
}

public interface IExporter
{
    ExportFormat Format { get; }
    string Extension { get; }
    void Write<T>(IEnumerable<T> rows, string path);
}
