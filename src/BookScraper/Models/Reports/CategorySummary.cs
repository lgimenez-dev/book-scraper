namespace BookScraper.Models.Reports;

public class CategorySummary
{
    public string Category { get; set; } = "";
    public int BookCount { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AvgPrice { get; set; }
    public double AvgRating { get; set; }
    public int TotalStock { get; set; }
}
