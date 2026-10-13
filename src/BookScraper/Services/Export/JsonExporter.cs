using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookScraper.Services.Export;

public class JsonExporter : IExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // keep "£", accents and quotes readable instead of \uXXXX escapes
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public ExportFormat Format => ExportFormat.Json;
    public string Extension => "json";

    public void Write<T>(IEnumerable<T> rows, string path)
    {
        using FileStream stream = File.Create(path);
        JsonSerializer.Serialize(stream, rows, Options);
    }
}
