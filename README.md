
<h2>Book Scraper</h2>
<p>
  <b>Description:</b> This project is a .NET console application that scrapes the full catalog of <a href="http://books.toscrape.com">books.toscrape.com</a>, stores every run in a local SQLite database and lets you analyze the data through an interactive menu with reports that can be exported to CSV or JSON.
</p>

<h3>Features:</h3>
<ul>
  <li><b>Interactive menu:</b> Selectable menu built with Spectre.Console (arrow keys + Enter).</li>
  <li><b>Flexible scraping:</b> Scrape the full catalog (1000 books) or pick one or more of the 50 categories. Pagination is followed automatically and detail pages are downloaded concurrently with retries.</li>
  <li><b>Rich data:</b> UPC, title, category, price (excl./incl. tax), tax, stock, rating (1–5), number of reviews, description, image URL and product URL.</li>
  <li><b>History:</b> Every scrape is saved as a run in <code>book-scraper.db</code> (SQLite), so reports don't need to scrape again.</li>
  <li><b>Reports:</b>
    <ul>
      <li><i>Category summary:</i> books, min/max/average price, average rating and total stock per category.</li>
      <li><i>Top / rankings:</i> most expensive, cheapest, best rated or most stock, with filters by category, minimum rating and price range.</li>
    </ul>
  </li>
  <li><b>Export:</b> Any report, or the full dataset of a run, can be exported to CSV or JSON in the <code>output/</code> folder.</li>
</ul>

<h3>Menu:</h3>
<pre>
Book Scraper
 ├─ Scrape          full catalog or selected categories
 ├─ Reports         category summary / top rankings (+ export)
 ├─ Export dataset  every book of a run to CSV / JSON
 ├─ Run history     list of saved runs
 └─ Exit
</pre>

<h3>Project structure:</h3>
<pre>
src/BookScraper/
  Models/             Book, Category, ScrapeRun, report models
  Services/Scraping/  HtmlFetcher (HTTP + retries), BookParser (XPath), ScraperService (orchestration)
  Services/Reports/   ReportService (SQL aggregations)
  Services/Export/    CsvExporter, JsonExporter, ExportService
  Data/               Database (SQLite schema), BookRepository
  UI/                 menus and table rendering
tests/BookScraper.Tests/  parser tests with saved HTML fixtures, report tests on in-memory SQLite
</pre>

<h3>Requirements:</h3>
<ul>
  <li>.NET 10.0</li>
  <li><a href="https://html-agility-pack.net/select-nodes">HtmlAgilityPack</a></li>
  <li><a href="https://joshclose.github.io/CsvHelper/">CsvHelper</a></li>
  <li><a href="https://spectreconsole.net/">Spectre.Console</a></li>
  <li><a href="https://github.com/DapperLib/Dapper">Dapper</a> + Microsoft.Data.Sqlite</li>
</ul>

<h3>Usage:</h3>
<ul>
  <li>Clone the repository</li>
  <li>Run the app with <code>dotnet run --project src/BookScraper</code></li>
  <li>Run the tests with <code>dotnet test</code></li>
  <li><code>book-scraper.db</code> and the <code>output/</code> folder are created in the directory where you run the app.</li>
</ul>

<h3>Sample output:</h3>
<img src="https://github.com/user-attachments/assets/bf9bbd97-2968-4173-a6f4-af3b92ed447e"/>
