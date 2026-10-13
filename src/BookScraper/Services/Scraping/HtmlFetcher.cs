using HtmlAgilityPack;

namespace BookScraper.Services.Scraping;

public interface IHtmlFetcher
{
    Task<HtmlDocument> GetAsync(Uri uri, CancellationToken ct = default);
}

public class HtmlFetcher(HttpClient http) : IHtmlFetcher
{
    private const int MaxAttempts = 3;

    public async Task<HtmlDocument> GetAsync(Uri uri, CancellationToken ct = default)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                string html = await http.GetStringAsync(uri, ct);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                return doc;
            }
            // network errors and HttpClient timeouts are retried with exponential backoff
            catch (Exception ex) when (attempt < MaxAttempts && !ct.IsCancellationRequested
                                       && ex is HttpRequestException or TaskCanceledException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1)), ct);
            }
        }
    }
}
