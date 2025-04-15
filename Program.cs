// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebCrawler.Data;
using WebCrawler.Services;

Console.WriteLine("Hello, World!");


// Setup configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup DI
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddHttpClient<ICrawlerService, CrawlerService>();
services.AddDbContext<CrawlerDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
services.AddScoped<ICrawlerService, CrawlerService>();

var serviceProvider = services.BuildServiceProvider();

// Ensure database is created
using (var scope = serviceProvider.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Run crawler
using (var scope = serviceProvider.CreateScope())
{
    var crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();
    await crawlerService.CrawlAsync();
}