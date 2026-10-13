using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using BookScraper.Models;

namespace BookScraper.Services.Scraping;

public partial class BookParser : IBookParser
{
    private static readonly Dictionary<string, int> RatingWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["One"] = 1, ["Two"] = 2, ["Three"] = 3, ["Four"] = 4, ["Five"] = 5,
    };

    [GeneratedRegex(@"\d+(?:[.,]\d+)?")]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex IntegerRegex();

    public IReadOnlyList<Category> ParseCategories(HtmlDocument doc, Uri pageUri)
    {
        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//div[@class='side_categories']/ul/li/ul/li/a");
        if (nodes == null)
        {
            return [];
        }
        return nodes
            .Select(a => new Category(Clean(a.InnerText), Resolve(pageUri, a.GetAttributeValue("href", ""))))
            .ToList();
    }

    public ListingPage ParseListing(HtmlDocument doc, Uri pageUri)
    {
        HtmlNodeCollection? links = doc.DocumentNode.SelectNodes("//article[@class='product_pod']/h3/a");
        List<string> bookUrls = links?
            .Select(a => Resolve(pageUri, a.GetAttributeValue("href", "")))
            .ToList() ?? [];

        string? nextHref = doc.DocumentNode.SelectSingleNode("//li[@class='next']/a")?.GetAttributeValue("href", null);
        string? nextUrl = string.IsNullOrEmpty(nextHref) ? null : Resolve(pageUri, nextHref);
        return new ListingPage(bookUrls, nextUrl);
    }

    public Book ParseBook(HtmlDocument doc, Uri pageUri)
    {
        HtmlNode root = doc.DocumentNode;
        HtmlNode main = root.SelectSingleNode("//div[contains(@class,'product_main')]")
            ?? throw new ParseException($"Product block not found in {pageUri}");

        string title = Clean(main.SelectSingleNode("h1")?.InnerText);
        if (title.Length == 0)
        {
            throw new ParseException($"Title not found in {pageUri}");
        }

        // product information table: <th>UPC</th><td>...</td>
        Dictionary<string, string> info = new(StringComparer.OrdinalIgnoreCase);
        foreach (HtmlNode row in root.SelectNodes("//table[contains(@class,'table-striped')]//tr") ?? Enumerable.Empty<HtmlNode>())
        {
            string key = Clean(row.SelectSingleNode("th")?.InnerText);
            if (key.Length > 0)
            {
                info[key] = Clean(row.SelectSingleNode("td")?.InnerText);
            }
        }

        string upc = info.GetValueOrDefault("UPC", "");
        if (upc.Length == 0)
        {
            throw new ParseException($"UPC not found in {pageUri}");
        }

        string? ratingClass = main.SelectSingleNode(".//p[contains(@class,'star-rating')]")?
            .GetClasses()
            .FirstOrDefault(RatingWords.ContainsKey);

        string? imageSrc = root.SelectSingleNode("//div[@id='product_gallery']//img")?.GetAttributeValue("src", null);
        string description = Clean(root.SelectSingleNode("//div[@id='product_description']/following-sibling::p")?.InnerText);
        string category = Clean(root.SelectSingleNode("//ul[@class='breadcrumb']/li[3]/a")?.InnerText);

        return new Book
        {
            Upc = upc,
            Title = title,
            Category = category.Length > 0 ? category : "Unknown",
            PriceExclTax = ParsePrice(info.GetValueOrDefault("Price (excl. tax)")),
            PriceInclTax = ParsePrice(info.GetValueOrDefault("Price (incl. tax)")),
            Tax = ParsePrice(info.GetValueOrDefault("Tax")),
            Stock = ParseInt(info.GetValueOrDefault("Availability")),
            Reviews = ParseInt(info.GetValueOrDefault("Number of reviews")),
            Rating = ratingClass != null ? RatingWords[ratingClass] : 0,
            Description = description.Length > 0 ? description : null,
            ImageUrl = string.IsNullOrEmpty(imageSrc) ? null : Resolve(pageUri, imageSrc),
            ProductUrl = pageUri.AbsoluteUri,
        };
    }

    // "£51.77" -> 51.77, independent of the system culture
    internal static decimal ParsePrice(string? raw)
    {
        Match match = PriceRegex().Match(raw ?? "");
        if (!match.Success)
        {
            return 0;
        }
        return decimal.Parse(match.Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    // "In stock (22 available)" -> 22, "Out of stock" -> 0
    internal static int ParseInt(string? raw)
    {
        Match match = IntegerRegex().Match(raw ?? "");
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : 0;
    }

    // merges the "base" URL with the "relative" link to form an "absolute" URL
    private static string Resolve(Uri baseUri, string href) => new Uri(baseUri, href).AbsoluteUri;

    private static string Clean(string? text) => HtmlEntity.DeEntitize(text ?? "").Trim();
}
