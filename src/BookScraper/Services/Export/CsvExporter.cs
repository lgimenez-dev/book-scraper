using System.Globalization;
using CsvHelper;

namespace BookScraper.Services.Export;

public class CsvExporter : IExporter
{
    public ExportFormat Format => ExportFormat.Csv;
    public string Extension => "csv";

    public void Write<T>(IEnumerable<T> rows, string path)
    {
        // one column per property, invariant culture so decimals always use "."
        using StreamWriter writer = new StreamWriter(path);
        using CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(rows);
    }
}
