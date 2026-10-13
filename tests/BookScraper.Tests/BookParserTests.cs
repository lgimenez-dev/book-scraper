using HtmlAgilityPack;
using BookScraper.Models;
using BookScraper.Services.Scraping;

namespace BookScraper.Tests;

public class BookParserTests
{
    private readonly BookParser _parser = new();

    private static HtmlDocument LoadFixture(string name)
    {
        HtmlDocument doc = new HtmlDocument();
        doc.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
        return doc;
    }

    [Fact]
    public void ParseCategories_ReturnsAllCategoriesWithAbsoluteUrls()
    {
        Uri page = new("http://books.toscrape.com/index.html");

        IReadOnlyList<Category> categories = _parser.ParseCategories(LoadFixture("index.html"), page);

        Assert.Equal(50, categories.Count);
        Assert.Contains(categories, c => c.Name == "Travel"
            && c.Url == "http://books.toscrape.com/catalogue/category/books/travel_2/index.html");
        Assert.DoesNotContain(categories, c => c.Name == "Books");
    }

    [Fact]
    public void ParseListing_FirstPage_ReturnsBooksAndNextPage()
    {
        Uri page = new("http://books.toscrape.com/catalogue/category/books/mystery_3/index.html");

        ListingPage listing = _parser.ParseListing(LoadFixture("category_page.html"), page);

        Assert.Equal(20, listing.BookUrls.Count);
        Assert.All(listing.BookUrls, url => Assert.StartsWith("http://books.toscrape.com/catalogue/", url));
        Assert.Equal("http://books.toscrape.com/catalogue/category/books/mystery_3/page-2.html", listing.NextPageUrl);
    }

    [Fact]
    public void ParseListing_LastPage_HasNoNextPage()
    {
        Uri page = new("http://books.toscrape.com/catalogue/category/books/mystery_3/page-2.html");

        ListingPage listing = _parser.ParseListing(LoadFixture("category_last_page.html"), page);

        Assert.Equal(12, listing.BookUrls.Count);
        Assert.Null(listing.NextPageUrl);
    }

    [Fact]
    public void ParseBook_ExtractsAllFields()
    {
        Uri page = new("http://books.toscrape.com/catalogue/sharp-objects_997/index.html");

        Book book = _parser.ParseBook(LoadFixture("book_detail.html"), page);

        Assert.Equal("e00eb4fd7b871a48", book.Upc);
        Assert.Equal("Sharp Objects", book.Title);
        Assert.Equal("Mystery", book.Category);
        Assert.Equal(47.82m, book.PriceExclTax);
        Assert.Equal(47.82m, book.PriceInclTax);
        Assert.Equal(0m, book.Tax);
        Assert.Equal(20, book.Stock);
        Assert.Equal(4, book.Rating);
        Assert.Equal(0, book.Reviews);
        Assert.StartsWith("WICKED above her hipbone", book.Description);
        Assert.Equal("http://books.toscrape.com/media/cache/c0/59/c05972805aa7201171b8fc71a5b00292.jpg", book.ImageUrl);
        Assert.Equal(page.AbsoluteUri, book.ProductUrl);
    }

    [Fact]
    public void ParseBook_PageWithoutProduct_Throws()
    {
        Uri page = new("http://books.toscrape.com/index.html");

        Assert.Throws<ParseException>(() => _parser.ParseBook(LoadFixture("index.html"), page));
    }

    [Theory]
    [InlineData("£51.77", 51.77)]
    [InlineData("Â£51.77", 51.77)]
    [InlineData("£1,5", 1.5)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void ParsePrice_IsCultureIndependent(string? raw, double expected)
    {
        Assert.Equal((decimal)expected, BookParser.ParsePrice(raw));
    }

    [Theory]
    [InlineData("In stock (22 available)", 22)]
    [InlineData("Out of stock", 0)]
    public void ParseInt_ExtractsStock(string raw, int expected)
    {
        Assert.Equal(expected, BookParser.ParseInt(raw));
    }
}
