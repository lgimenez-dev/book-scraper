using BookScraper.Data;
using BookScraper.Models;
using BookScraper.Models.Reports;
using BookScraper.Services.Export;
using BookScraper.Services.Reports;
using Spectre.Console;

namespace BookScraper.UI;

public class ReportsMenu(BookRepository repository, ReportService reports, ExportService exportService)
{
    private const string CategorySummaryOption = "Category summary";
    private const string RankingOption = "Top / rankings";
    private const string ChangeRunOption = "Change run";
    private const string AnyOption = "(all)";

    private static readonly Dictionary<RankingCriterion, string> CriterionLabels = new()
    {
        [RankingCriterion.MostExpensive] = "Most expensive",
        [RankingCriterion.Cheapest] = "Cheapest",
        [RankingCriterion.BestRated] = "Best rated",
        [RankingCriterion.MostStock] = "Most stock",
    };

    public void Show()
    {
        ScrapeRun? run = Prompts.SelectRun(repository);
        while (run != null)
        {
            string choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title($"[bold]Reports[/] — run {Markup.Escape(TableRenderer.DescribeRun(run))}")
                .AddChoices(CategorySummaryOption, RankingOption, ChangeRunOption, Prompts.Back));

            switch (choice)
            {
                case CategorySummaryOption:
                    ShowCategorySummary(run);
                    break;
                case RankingOption:
                    ShowRanking(run);
                    break;
                case ChangeRunOption:
                    run = Prompts.SelectRun(repository);
                    break;
                default:
                    return;
            }
        }
    }

    private void ShowCategorySummary(ScrapeRun run)
    {
        IReadOnlyList<CategorySummary> rows = reports.GetCategorySummary(run.Id);
        TableRenderer.RenderCategorySummary(rows);
        Prompts.OfferExport(exportService, rows, $"category_summary_run{run.Id}");
    }

    private void ShowRanking(ScrapeRun run)
    {
        RankingQuery query = new RankingQuery
        {
            Criterion = AnsiConsole.Prompt(new SelectionPrompt<RankingCriterion>()
                .Title("Ranking criterion")
                .UseConverter(c => CriterionLabels[c])
                .AddChoices(CriterionLabels.Keys)),
            Top = AnsiConsole.Prompt(new TextPrompt<int>("How many books to show?")
                .DefaultValue(10)
                .Validate(n => n is >= 1 and <= 1000
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Between 1 and 1000[/]"))),
        };

        string category = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Filter by category")
            .PageSize(15)
            .EnableSearch()
            .UseConverter(Markup.Escape)
            .AddChoices([AnyOption, .. repository.GetCategories(run.Id)]));
        query.Category = category == AnyOption ? null : category;

        int minRating = AnsiConsole.Prompt(new SelectionPrompt<int>()
            .Title("Minimum rating")
            .UseConverter(r => r == 0 ? "(any)" : $"{new string('★', r)} or more")
            .AddChoices(0, 1, 2, 3, 4, 5));
        query.MinRating = minRating == 0 ? null : minRating;

        query.MinPrice = Prompts.OptionalDecimal("Minimum price (£)");
        query.MaxPrice = Prompts.OptionalDecimal("Maximum price (£)");

        IReadOnlyList<Book> books = reports.GetRanking(run.Id, query);
        TableRenderer.RenderBooks(books, $"Top {query.Top} — {CriterionLabels[query.Criterion]}{DescribeFilters(query)}");
        if (books.Count > 0)
        {
            string criterion = query.Criterion.ToString().ToLowerInvariant();
            Prompts.OfferExport(exportService, books, $"ranking_{criterion}_run{run.Id}");
        }
    }

    private static string DescribeFilters(RankingQuery query)
    {
        List<string> filters = [];
        if (query.Category != null) filters.Add(query.Category);
        if (query.MinRating != null) filters.Add($"rating ≥ {query.MinRating}");
        if (query.MinPrice != null) filters.Add($"price ≥ £{query.MinPrice}");
        if (query.MaxPrice != null) filters.Add($"price ≤ £{query.MaxPrice}");
        return filters.Count == 0 ? "" : $" ({string.Join(", ", filters)})";
    }
}
