using BookScraper.Data;
using Spectre.Console;

namespace BookScraper.UI;

public class MainMenu(
    ScrapeMenu scrapeMenu,
    ReportsMenu reportsMenu,
    ExportMenu exportMenu,
    BookRepository repository)
{
    private const string ScrapeOption = "Scrape";
    private const string ReportsOption = "Reports";
    private const string ExportOption = "Export dataset";
    private const string RunsOption = "Run history";
    private const string ExitOption = "Exit";

    public async Task RunAsync()
    {
        AnsiConsole.Write(new FigletText("Book Scraper").Color(Color.Teal));
        AnsiConsole.MarkupLine("[grey]books.toscrape.com · analysis & reports[/]");

        while (true)
        {
            AnsiConsole.WriteLine();
            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[bold]What would you like to do?[/]")
                .AddChoices(ScrapeOption, ReportsOption, ExportOption, RunsOption, ExitOption));

            try
            {
                switch (choice)
                {
                    case ScrapeOption:
                        await scrapeMenu.ShowAsync();
                        break;
                    case ReportsOption:
                        reportsMenu.Show();
                        break;
                    case ExportOption:
                        exportMenu.Show();
                        break;
                    case RunsOption:
                        TableRenderer.RenderRuns(repository.GetRuns());
                        break;
                    default:
                        return;
                }
            }
            // keep the app alive after unexpected errors (network down, file locked, ...)
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            }
        }
    }
}
