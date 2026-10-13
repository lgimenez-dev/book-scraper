using System.Text;
using Microsoft.Extensions.DependencyInjection;
using BookScraper.Data;
using BookScraper.Services.Export;
using BookScraper.Services.Reports;
using BookScraper.Services.Scraping;
using BookScraper.UI;

Console.OutputEncoding = Encoding.UTF8;

ServiceCollection services = new ServiceCollection();

services.AddHttpClient<IHtmlFetcher, HtmlFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BookScraper/2.0");
});
services.AddSingleton<IBookParser, BookParser>();
services.AddTransient<ScraperService>();

services.AddSingleton(new Database("Data Source=book-scraper.db"));
services.AddSingleton<BookRepository>();
services.AddSingleton<ReportService>();

services.AddSingleton<IExporter, CsvExporter>();
services.AddSingleton<IExporter, JsonExporter>();
services.AddSingleton(sp => new ExportService(sp.GetServices<IExporter>(), "output"));

services.AddTransient<ScrapeMenu>();
services.AddTransient<ReportsMenu>();
services.AddTransient<ExportMenu>();
services.AddTransient<MainMenu>();

using ServiceProvider provider = services.BuildServiceProvider();
provider.GetRequiredService<Database>().EnsureCreated();
await provider.GetRequiredService<MainMenu>().RunAsync();
