using Dapper;
using Microsoft.Data.Sqlite;

namespace BookScraper.Data;

public class Database
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS scrape_runs (
            id          INTEGER PRIMARY KEY AUTOINCREMENT,
            started_at  TEXT    NOT NULL,
            finished_at TEXT,
            scope       TEXT    NOT NULL,
            book_count  INTEGER NOT NULL DEFAULT 0,
            error_count INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS books (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            run_id         INTEGER NOT NULL REFERENCES scrape_runs(id) ON DELETE CASCADE,
            upc            TEXT    NOT NULL,
            title          TEXT    NOT NULL,
            category       TEXT    NOT NULL,
            price_excl_tax REAL    NOT NULL,
            price_incl_tax REAL    NOT NULL,
            tax            REAL    NOT NULL,
            stock          INTEGER NOT NULL,
            rating         INTEGER NOT NULL,
            reviews        INTEGER NOT NULL,
            description    TEXT,
            image_url      TEXT,
            product_url    TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_books_run_category ON books(run_id, category);
        """;

    private readonly string _connectionString;

    // maps snake_case columns (price_incl_tax) to PascalCase properties (PriceInclTax)
    static Database()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqliteConnection Open()
    {
        SqliteConnection connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void EnsureCreated()
    {
        using SqliteConnection connection = Open();
        connection.Execute(Schema);
    }
}
