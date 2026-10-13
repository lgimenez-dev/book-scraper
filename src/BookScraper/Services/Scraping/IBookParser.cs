using HtmlAgilityPack;
using BookScraper.Models;

namespace BookScraper.Services.Scraping;

// result of parsing a listing page: book links and the link to the next page (if any)
public record ListingPage(IReadOnlyList<string> BookUrls, string? NextPageUrl);

public interface IBookParser
{
    IReadOnlyList<Category> ParseCategories(HtmlDocument doc, Uri pageUri);
    ListingPage ParseListing(HtmlDocument doc, Uri pageUri);
    Book ParseBook(HtmlDocument doc, Uri pageUri);
}

public class ParseException(string message) : Exception(message);
