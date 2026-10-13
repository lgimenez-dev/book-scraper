using System.Globalization;
using BookScraper.Models;
using BookScraper.Models.Reports;
using Spectre.Console;

namespace BookScraper.UI;

public static class TableRenderer
{
    public static void RenderCategorySummary(IReadOnlyList<CategorySummary> rows)
    {
        Table table = new Table().Border(TableBorder.Rounded).Title("[bold]Category summary[/]")
            .AddColumn("Category")
            .AddColumn(new TableColumn("Books").RightAligned())
            .AddColumn(new TableColumn("Min price").RightAligned())
            .AddColumn(new TableColumn("Max price").RightAligned())
            .AddColumn(new TableColumn("Avg price").RightAligned())
            .AddColumn(new TableColumn("Avg rating").RightAligned())
            .AddColumn(new TableColumn("Total stock").RightAligned());

        foreach (CategorySummary row in rows)
        {
            table.AddRow(
                Markup.Escape(row.Category),
                row.BookCount.ToString(CultureInfo.InvariantCulture),
                Price(row.MinPrice),
                Price(row.MaxPrice),
                Price(row.AvgPrice),
                row.AvgRating.ToString("F2", CultureInfo.InvariantCulture),
                row.TotalStock.ToString(CultureInfo.InvariantCulture));
        }

        table.Caption($"{rows.Count} categories · {rows.Sum(r => r.BookCount)} books");
        AnsiConsole.Write(table);
    }

    public static void RenderBooks(IReadOnlyList<Book> books, string title)
    {
        Table table = new Table().Border(TableBorder.Rounded).Title($"[bold]{Markup.Escape(title)}[/]")
            .AddColumn(new TableColumn("#").RightAligned())
            .AddColumn("Title")
            .AddColumn("Category")
            .AddColumn(new TableColumn("Price").RightAligned())
            .AddColumn("Rating")
            .AddColumn(new TableColumn("Stock").RightAligned())
            .AddColumn("UPC");

        for (int i = 0; i < books.Count; i++)
        {
            Book book = books[i];
            table.AddRow(
                (i + 1).ToString(CultureInfo.InvariantCulture),
                Markup.Escape(Truncate(book.Title, 50)),
                Markup.Escape(book.Category),
                Price(book.PriceInclTax),
                Stars(book.Rating),
                book.Stock.ToString(CultureInfo.InvariantCulture),
                Markup.Escape(book.Upc));
        }

        if (books.Count == 0)
        {
            table.Caption("[yellow]No books match the filters[/]");
        }
        AnsiConsole.Write(table);
    }

    public static void RenderRuns(IReadOnlyList<ScrapeRun> runs)
    {
        Table table = new Table().Border(TableBorder.Rounded).Title("[bold]Run history[/]")
            .AddColumn(new TableColumn("#").RightAligned())
            .AddColumn("Started")
            .AddColumn(new TableColumn("Duration").RightAligned())
            .AddColumn("Scope")
            .AddColumn(new TableColumn("Books").RightAligned())
            .AddColumn(new TableColumn("Errors").RightAligned());

        foreach (ScrapeRun run in runs)
        {
            string duration = run.FinishedAt is DateTime end
                ? (end - run.StartedAt).TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s"
                : "-";
            table.AddRow(
                run.Id.ToString(CultureInfo.InvariantCulture),
                run.StartedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                duration,
                Markup.Escape(Truncate(run.Scope, 50)),
                run.BookCount.ToString(CultureInfo.InvariantCulture),
                run.ErrorCount > 0 ? $"[red]{run.ErrorCount}[/]" : "0");
        }
        AnsiConsole.Write(table);
    }

    public static string DescribeRun(ScrapeRun run) =>
        $"#{run.Id} · {run.StartedAt:yyyy-MM-dd HH:mm} · {Truncate(run.Scope, 40)} · {run.BookCount} books";

    private static string Price(decimal value) => "£" + value.ToString("F2", CultureInfo.InvariantCulture);

    private static string Stars(int rating) =>
        rating <= 0 ? "-" : $"[yellow]{new string('★', rating)}[/][grey]{new string('☆', 5 - rating)}[/]";

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}
