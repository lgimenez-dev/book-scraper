using Microsoft.Data.Sqlite;
using BookScraper.Data;
using BookScraper.Models;
using BookScraper.Models.Reports;
using BookScraper.Services.Reports;

namespace BookScraper.Tests;

public class ReportServiceTests : IDisposable
{
    // a shared in-memory database lives while at least one connection stays open
    private readonly SqliteConnection _keepAlive;
    private readonly BookRepository _repository;
    private readonly ReportService _reports;
    private readonly long _runId;

    public ReportServiceTests()
    {
        string connectionString = $"Data Source=test_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();

        Database db = new Database(connectionString);
        db.EnsureCreated();
        _repository = new BookRepository(db);
        _reports = new ReportService(db);

        _runId = _repository.SaveRun(
            new ScrapeRun { StartedAt = new DateTime(2026, 10, 12, 10, 0, 0), FinishedAt = new DateTime(2026, 10, 12, 10, 1, 0), Scope = "test" },
            [
                NewBook("A", "Travel", 10.50m, rating: 3, stock: 5),
                NewBook("B", "Travel", 30.00m, rating: 5, stock: 1),
                NewBook("C", "Mystery", 20.25m, rating: 1, stock: 10),
                NewBook("D", "Mystery", 55.99m, rating: 4, stock: 2),
                NewBook("E", "Poetry", 5.00m, rating: 2, stock: 7),
            ]);
    }

    public void Dispose() => _keepAlive.Dispose();

    private static Book NewBook(string title, string category, decimal price, int rating, int stock) => new()
    {
        Upc = "upc-" + title,
        Title = title,
        Category = category,
        PriceExclTax = price,
        PriceInclTax = price,
        Rating = rating,
        Stock = stock,
        ProductUrl = "http://example/" + title,
    };

    [Fact]
    public void Repository_RoundTripsRunsAndBooks()
    {
        ScrapeRun run = Assert.Single(_repository.GetRuns());
        Assert.Equal(_runId, run.Id);
        Assert.Equal(new DateTime(2026, 10, 12, 10, 0, 0), run.StartedAt);

        IReadOnlyList<Book> books = _repository.GetBooks(_runId);
        Assert.Equal(["A", "B", "C", "D", "E"], books.Select(b => b.Title));
        Assert.Equal(20.25m, books[2].PriceInclTax);
        Assert.Equal(["Mystery", "Poetry", "Travel"], _repository.GetCategories(_runId));
    }

    [Fact]
    public void CategorySummary_AggregatesPerCategory()
    {
        IReadOnlyList<CategorySummary> summary = _reports.GetCategorySummary(_runId);

        Assert.Equal(["Mystery", "Poetry", "Travel"], summary.Select(s => s.Category));
        CategorySummary travel = summary.Single(s => s.Category == "Travel");
        Assert.Equal(2, travel.BookCount);
        Assert.Equal(10.50m, travel.MinPrice);
        Assert.Equal(30.00m, travel.MaxPrice);
        Assert.Equal(20.25m, travel.AvgPrice);
        Assert.Equal(4.0, travel.AvgRating);
        Assert.Equal(6, travel.TotalStock);
    }

    [Theory]
    [InlineData(RankingCriterion.MostExpensive, new[] { "D", "B", "C" })]
    [InlineData(RankingCriterion.Cheapest, new[] { "E", "A", "C" })]
    [InlineData(RankingCriterion.BestRated, new[] { "B", "D", "A" })]
    [InlineData(RankingCriterion.MostStock, new[] { "C", "E", "A" })]
    public void Ranking_OrdersByCriterion(RankingCriterion criterion, string[] expected)
    {
        IReadOnlyList<Book> top = _reports.GetRanking(_runId, new RankingQuery { Criterion = criterion, Top = 3 });

        Assert.Equal(expected, top.Select(b => b.Title));
    }

    [Fact]
    public void Ranking_AppliesFilters()
    {
        RankingQuery query = new RankingQuery
        {
            Criterion = RankingCriterion.Cheapest,
            MinRating = 2,
            MinPrice = 10m,
            MaxPrice = 50m,
        };
        Assert.Equal(["A", "B"], _reports.GetRanking(_runId, query).Select(b => b.Title));

        query.Category = "Travel";
        query.MinRating = 4;
        Assert.Equal(["B"], _reports.GetRanking(_runId, query).Select(b => b.Title));
    }

    [Fact]
    public void Reports_OnlyIncludeTheRequestedRun()
    {
        long otherRun = _repository.SaveRun(new ScrapeRun { StartedAt = DateTime.Now, Scope = "other" },
            [NewBook("Z", "Travel", 999m, rating: 5, stock: 99)]);

        Assert.DoesNotContain(_reports.GetRanking(_runId, new RankingQuery { Top = 100 }), b => b.Title == "Z");
        Assert.Equal(["Z"], _reports.GetRanking(otherRun, new RankingQuery()).Select(b => b.Title));
    }
}
