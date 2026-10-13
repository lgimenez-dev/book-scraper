namespace BookScraper.Models.Reports;

public enum RankingCriterion
{
    MostExpensive,
    Cheapest,
    BestRated,
    MostStock,
}

public class RankingQuery
{
    public RankingCriterion Criterion { get; set; } = RankingCriterion.MostExpensive;
    public int Top { get; set; } = 10;
    // optional filters, null means "no filter"
    public string? Category { get; set; }
    public int? MinRating { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}
