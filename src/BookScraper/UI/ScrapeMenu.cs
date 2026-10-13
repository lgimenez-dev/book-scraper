using System.Diagnostics;
using System.Globalization;
using BookScraper.Models;
using BookScraper.Services.Scraping;
using Spectre.Console;

namespace BookScraper.UI;

public class ScrapeMenu(ScraperService scraper)
{
    private const string FullCatalog = "Full catalog (1000 books)";
    private const string PickCategories = "Pick categories";

    public async Task ShowAsync()
    {
        string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("[bold]Scrape[/] — what do you want to download?")
            .AddChoices(FullCatalog, PickCategories, Prompts.Back));

        IReadOnlyList<Category>? categories = null;
        if (choice == Prompts.Back)
        {
            return;
        }
        if (choice == PickCategories)
        {
            categories = await PromptCategoriesAsync();
            if (categories == null)
            {
                return;
            }
        }
        await RunScrapeAsync(categories);
    }

    private async Task<IReadOnlyList<Category>?> PromptCategoriesAsync()
    {
        IReadOnlyList<Category> all = await AnsiConsole.Status()
            .StartAsync("Fetching categories...", _ => scraper.GetCategoriesAsync());
        if (all.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No categories were found on the site.[/]");
            return null;
        }

        return AnsiConsole.Prompt(new MultiSelectionPrompt<Category>()
            .Title("Pick one or more categories")
            .InstructionsText("[grey](Space to toggle, Enter to confirm)[/]")
            .PageSize(15)
            .MoreChoicesText("[grey](Move up/down to see more categories)[/]")
            .UseConverter(c => Markup.Escape(c.Name))
            .AddChoices(all));
    }

    private async Task RunScrapeAsync(IReadOnlyList<Category>? categories)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ScrapeResult result = await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(),
                     new RemainingTimeColumn(), new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                ProgressTask listing = ctx.AddTask("Finding books").IsIndeterminate();
                ProgressTask details = ctx.AddTask("Downloading details", autoStart: false);
                object gate = new();

                return await scraper.ScrapeAsync(categories, progress =>
                {
                    // callbacks come from several threads during the parallel download
                    lock (gate)
                    {
                        if (progress.Total is int total)
                        {
                            listing.IsIndeterminate(false).Value(listing.MaxValue).StopTask();
                            details.MaxValue = Math.Max(total, 1);
                            details.StartTask();
                            details.Value = Math.Max(details.Value, progress.Completed);
                        }
                        else
                        {
                            listing.Description = $"Finding books ({progress.Completed})";
                        }
                    }
                });
            });
        stopwatch.Stop();

        string seconds = stopwatch.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture);
        AnsiConsole.MarkupLine($"[green]✓ Run #{result.Run.Id} saved:[/] {result.Run.BookCount} books in {seconds} s");
        if (result.Errors.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]{result.Errors.Count} errors[/] (showing up to 5):");
            foreach (string error in result.Errors.Take(5))
            {
                AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(error)}[/]");
            }
        }
    }
}
