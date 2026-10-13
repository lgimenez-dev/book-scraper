namespace BookScraper.Services.Export;

public class ExportService(IEnumerable<IExporter> exporters, string outputDirectory)
{
    // writes the rows to <outputDirectory>/<baseName>_<timestamp>.<ext> and returns the full path
    public string Export<T>(IEnumerable<T> rows, string baseName, ExportFormat format)
    {
        IExporter exporter = exporters.First(e => e.Format == format);
        Directory.CreateDirectory(outputDirectory);
        string path = Path.GetFullPath(Path.Combine(
            outputDirectory, $"{baseName}_{DateTime.Now:yyyyMMdd_HHmmss}.{exporter.Extension}"));
        exporter.Write(rows, path);
        return path;
    }
}
