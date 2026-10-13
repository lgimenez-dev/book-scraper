using System.Globalization;
using BookScraper.Data;
using BookScraper.Models;
using BookScraper.Services.Export;
using Spectre.Console;

namespace BookScraper.UI;

// prompts shared by several menus
public static class Prompts
{
    public const string Back = "← Back";

    // lets the user pick a run (the most recent one is first); null when there are no runs
    public static ScrapeRun? SelectRun(BookRepository repository)
    {
        IReadOnlyList<ScrapeRun> runs = repository.GetRuns();
        if (runs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]There are no saved runs yet. Use \"Scrape\" first.[/]");
            return null;
        }
        if (runs.Count == 1)
        {
            return runs[0];
        }
        return AnsiConsole.Prompt(new SelectionPrompt<ScrapeRun>()
            .Title("Which run?")
            .PageSize(10)
            .UseConverter(r => Markup.Escape(TableRenderer.DescribeRun(r)))
            .AddChoices(runs));
    }

    // offers to export the rows until the user goes back
    public static void OfferExport<T>(ExportService exportService, IEnumerable<T> rows, string baseName)
    {
        const string csv = "Export to CSV";
        const string json = "Export to JSON";
        while (true)
        {
            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Export this report?")
                .AddChoices(csv, json, Back));
            if (choice == Back)
            {
                return;
            }
            string path = exportService.Export(rows, baseName, choice == csv ? ExportFormat.Csv : ExportFormat.Json);
            AnsiConsole.MarkupLine($"[green]✓ Exported:[/] {Markup.Escape(path)}");
        }
    }

    // empty input means "no value"; accepts both "12.5" and "12,5"
    public static decimal? OptionalDecimal(string question)
    {
        string input = AnsiConsole.Prompt(new TextPrompt<string>($"{question} [grey](Enter = no limit)[/]")
            .AllowEmpty()
            .Validate(text => string.IsNullOrWhiteSpace(text) || TryParseDecimal(text, out _)
                ? ValidationResult.Success()
                : ValidationResult.Error("[red]Enter a valid number[/]")));
        return TryParseDecimal(input, out decimal value) ? value : null;
    }

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        && value >= 0;
}
