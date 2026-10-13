using Dapper;
using Microsoft.Data.Sqlite;
using BookScraper.Data;
using BookScraper.Models;
using BookScraper.Models.Reports;

namespace BookScraper.Services.Reports;

public class ReportService(Database db)
{
    public IReadOnlyList<CategorySummary> GetCategorySummary(long runId)
    {
        using SqliteConnection connection = db.Open();
        return connection.Query<CategorySummary>("""
            SELECT category,
                   COUNT(*)                     AS book_count,
                   MIN(price_incl_tax)          AS min_price,
                   MAX(price_incl_tax)          AS max_price,
                   ROUND(AVG(price_incl_tax), 2) AS avg_price,
                   ROUND(AVG(rating), 2)        AS avg_rating,
                   SUM(stock)                   AS total_stock
            FROM books
            WHERE run_id = @runId
            GROUP BY category
            ORDER BY category
            """, new { runId }).ToList();
    }

    public IReadOnlyList<Book> GetRanking(long runId, RankingQuery query)
    {
        // the ORDER BY comes from a fixed whitelist, never from user input
        string orderBy = query.Criterion switch
        {
            RankingCriterion.MostExpensive => "price_incl_tax DESC, title",
            RankingCriterion.Cheapest => "price_incl_tax ASC, title",
            RankingCriterion.BestRated => "rating DESC, reviews DESC, title",
            RankingCriterion.MostStock => "stock DESC, title",
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.Criterion, null),
        };

        using SqliteConnection connection = db.Open();
        return connection.Query<Book>($"""
            SELECT {BookRepository.BookColumns}
            FROM books
            WHERE run_id = @RunId
              AND (@Category IS NULL OR category = @Category)
              AND (@MinRating IS NULL OR rating >= @MinRating)
              AND (@MinPrice IS NULL OR price_incl_tax >= @MinPrice)
              AND (@MaxPrice IS NULL OR price_incl_tax <= @MaxPrice)
            ORDER BY {orderBy}
            LIMIT @Top
            """,
            new { RunId = runId, query.Category, query.MinRating, query.MinPrice, query.MaxPrice, query.Top })
            .ToList();
    }
}
