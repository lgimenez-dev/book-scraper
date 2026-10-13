using BookScraper.Data;
using BookScraper.Models;
using BookScraper.Services.Export;
using Spectre.Console;

namespace BookScraper.UI;

// exports every book of a run with all its columns
public class ExportMenu(BookRepository repository, ExportService exportService)
{
    public void Show()
    {
        ScrapeRun? run = Prompts.SelectRun(repository);
        if (run == null)
        {
            return;
        }
        IReadOnlyList<Book> books = repository.GetBooks(run.Id);
        AnsiConsole.MarkupLine($"Run {Markup.Escape(TableRenderer.DescribeRun(run))}");
        Prompts.OfferExport(exportService, books, $"dataset_run{run.Id}");
    }
}
