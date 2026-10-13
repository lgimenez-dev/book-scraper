using System.Collections.Concurrent;
using BookScraper.Data;
using BookScraper.Models;

namespace BookScraper.Services.Scraping;

public record ScrapeProgress(string Stage, int Completed, int? Total);

public record ScrapeResult(ScrapeRun Run, IReadOnlyList<string> Errors);

public class ScraperService(IHtmlFetcher fetcher, IBookParser parser, BookRepository repository)
{
    public static readonly Uri HomeUri = new("http://books.toscrape.com/index.html");
    public const string FullCatalogScope = "Full catalog";
    private const int MaxConcurrency = 8;

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return parser.ParseCategories(await fetcher.GetAsync(HomeUri, ct), HomeUri);
    }

    // scrapes the given categories (or the full catalog when null) and saves the result as a new run
    public async Task<ScrapeResult> ScrapeAsync(
        IReadOnlyList<Category>? categories,
        Action<ScrapeProgress>? onProgress = null,
        CancellationToken ct = default)
    {
        ScrapeRun run = new ScrapeRun
        {
            StartedAt = DateTime.Now,
            Scope = categories == null ? FullCatalogScope : string.Join(", ", categories.Select(c => c.Name)),
        };
        ConcurrentQueue<string> errors = new();

        // 1. walk the listing pages (following "next") to collect the book links
        IEnumerable<Uri> startPages = categories == null ? [HomeUri] : categories.Select(c => new Uri(c.Url));
        List<string> bookUrls = [];
        foreach (Uri startPage in startPages)
        {
            Uri? page = startPage;
            while (page != null)
            {
                try
                {
                    ListingPage listing = parser.ParseListing(await fetcher.GetAsync(page, ct), page);
                    bookUrls.AddRange(listing.BookUrls);
                    page = listing.NextPageUrl == null ? null : new Uri(listing.NextPageUrl);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors.Enqueue($"{page}: {ex.Message}");
                    page = null;
                }
                onProgress?.Invoke(new ScrapeProgress("Finding books", bookUrls.Count, null));
            }
        }
        bookUrls = bookUrls.Distinct().ToList();

        // 2. download the detail pages concurrently, keeping the listing order
        Book?[] books = new Book?[bookUrls.Count];
        int completed = 0;
        onProgress?.Invoke(new ScrapeProgress("Downloading details", 0, bookUrls.Count));
        await Parallel.ForEachAsync(
            Enumerable.Range(0, bookUrls.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency, CancellationToken = ct },
            async (i, token) =>
            {
                Uri uri = new Uri(bookUrls[i]);
                try
                {
                    books[i] = parser.ParseBook(await fetcher.GetAsync(uri, token), uri);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors.Enqueue($"{uri}: {ex.Message}");
                }
                onProgress?.Invoke(new ScrapeProgress("Downloading details", Interlocked.Increment(ref completed), bookUrls.Count));
            });

        List<Book> scraped = books.OfType<Book>().ToList();
        run.FinishedAt = DateTime.Now;
        run.BookCount = scraped.Count;
        run.ErrorCount = errors.Count;
        run.Id = repository.SaveRun(run, scraped);
        return new ScrapeResult(run, errors.ToList());
    }
}
