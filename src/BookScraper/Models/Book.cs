namespace BookScraper.Models;

public class Book
{
    public string Upc { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal PriceExclTax { get; set; }
    public decimal PriceInclTax { get; set; }
    public decimal Tax { get; set; }
    public int Stock { get; set; }
    // 1 to 5 stars, 0 when the page has no rating
    public int Rating { get; set; }
    public int Reviews { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string ProductUrl { get; set; } = "";
}
