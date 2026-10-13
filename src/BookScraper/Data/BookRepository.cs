using Dapper;
using Microsoft.Data.Sqlite;
using BookScraper.Models;

namespace BookScraper.Data;

public class BookRepository(Database db)
{
    internal const string BookColumns = """
        upc, title, category, price_excl_tax, price_incl_tax, tax,
        stock, rating, reviews, description, image_url, product_url
        """;

    // saves the run and its books in a single transaction and returns the new run id
    public long SaveRun(ScrapeRun run, IEnumerable<Book> books)
    {
        using SqliteConnection connection = db.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();

        long runId = connection.ExecuteScalar<long>("""
            INSERT INTO scrape_runs (started_at, finished_at, scope, book_count, error_count)
            VALUES (@StartedAt, @FinishedAt, @Scope, @BookCount, @ErrorCount)
            RETURNING id
            """, run, transaction);

        connection.Execute($"""
            INSERT INTO books (run_id, {BookColumns})
            VALUES (@RunId, @Upc, @Title, @Category, @PriceExclTax, @PriceInclTax, @Tax,
                    @Stock, @Rating, @Reviews, @Description, @ImageUrl, @ProductUrl)
            """,
            books.Select(b => new
            {
                RunId = runId, b.Upc, b.Title, b.Category, b.PriceExclTax, b.PriceInclTax, b.Tax,
                b.Stock, b.Rating, b.Reviews, b.Description, b.ImageUrl, b.ProductUrl,
            }),
            transaction);

        transaction.Commit();
        return runId;
    }

    // most recent first
    public IReadOnlyList<ScrapeRun> GetRuns()
    {
        using SqliteConnection connection = db.Open();
        return connection.Query<ScrapeRun>("SELECT * FROM scrape_runs ORDER BY id DESC").ToList();
    }

    public IReadOnlyList<Book> GetBooks(long runId)
    {
        using SqliteConnection connection = db.Open();
        return connection.Query<Book>(
            $"SELECT {BookColumns} FROM books WHERE run_id = @runId ORDER BY id", new { runId }).ToList();
    }

    public IReadOnlyList<string> GetCategories(long runId)
    {
        using SqliteConnection connection = db.Open();
        return connection.Query<string>(
            "SELECT DISTINCT category FROM books WHERE run_id = @runId ORDER BY category", new { runId }).ToList();
    }
}
