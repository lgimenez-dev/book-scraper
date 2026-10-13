namespace BookScraper.Models;

public class ScrapeRun
{
    public long Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    // "Full catalog" or the list of scraped categories
    public string Scope { get; set; } = "";
    public int BookCount { get; set; }
    public int ErrorCount { get; set; }
}
