using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebCrawler.Data;
using WebCrawler.Models;

namespace WebCrawler.Services
{
    public class CrawlerService : ICrawlerService
    {
        private readonly HttpClient _httpClient;
        private readonly CrawlerDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly List<string> _seedUrls;
        private readonly int _crawlDelaySeconds;
        private readonly int _maxDepth;
        private readonly int _maxPages;

        


        public CrawlerService(HttpClient httpClient, CrawlerDbContext dbContext, IConfiguration configuration)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);

            //_httpClient = httpClient;
            _dbContext = dbContext;
            _configuration = configuration;

            _seedUrls = _configuration.GetSection("CrawlerSettings:SeedUrls").Get<List<string>>() ?? new List<string>();
            _crawlDelaySeconds = _configuration.GetValue<int>("CrawlerSettings:CrawlDelaySeconds");
            _maxDepth = _configuration.GetValue<int>("CrawlerSettings:MaxDepth");
            _maxPages = _configuration.GetValue<int>("CrawlerSettings:MaxPages");
        }

        public async Task CrawlAsync()
        {
            var visitedUrls = new HashSet<string>();
            var urlsToVisit = new Queue<(string Url, int Depth)>();
            foreach (var url in _seedUrls)
            {
                urlsToVisit.Enqueue((url, 0));
            }

            int pagesCrawled = 0;

            while (urlsToVisit.Count > 0 && pagesCrawled < _maxPages)
            {
                var (currentUrl, currentDepth) = urlsToVisit.Dequeue();

                if (visitedUrls.Contains(currentUrl) || currentDepth > _maxDepth)
                    continue;

                try
                {
                    var html = await _httpClient.GetStringAsync(currentUrl);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    // Extract title
                    var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "No Title";

                    // Extract meta description
                    var metaDescription = doc.DocumentNode
                        .SelectSingleNode("//meta[@name='description']")
                        ?.GetAttributeValue("content", "")?.Trim() ?? "No Meta Description";

                    // Extract body content (plain text)
                    var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
                    var bodyContent = bodyNode != null ? Regex.Replace(bodyNode.InnerText, @"\s+", " ").Trim() : "No Body Content";
                    // Limit body content to 5000 characters to avoid excessive storage
                    bodyContent = bodyContent.Length > 5000 ? bodyContent.Substring(0, 5000) : bodyContent;

                    // Save to database
                    var page = new CrawledPage
                    {
                        Url = currentUrl,
                        Title = title,
                        MetaDescription = metaDescription,
                        BodyContent = bodyContent,
                        CrawledAt = DateTime.UtcNow
                    };

                    _dbContext.CrawledPages.Add(page);
                    await _dbContext.SaveChangesAsync();

                    visitedUrls.Add(currentUrl);
                    pagesCrawled++;

                    // Extract links
                    var links = doc.DocumentNode.SelectNodes("//a[@href]")
                        ?.Select(node => node.GetAttributeValue("href", ""))
                        .Where(href => href.StartsWith("http") && href.Contains(new Uri(currentUrl).Host))
                        .Distinct();

                    if (links != null)
                    {
                        foreach (var link in links)
                        {
                            if (!visitedUrls.Contains(link))
                            {
                                urlsToVisit.Enqueue((link, currentDepth + 1));
                            }
                        }
                    }

                    Console.WriteLine($"Crawled {currentUrl} (Title: {title})");

                    // Polite delay
                    await Task.Delay(_crawlDelaySeconds * 1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error crawling {currentUrl}: {ex.Message}");
                }
            }

            Console.WriteLine($"Crawling completed. Total pages crawled: {pagesCrawled}");
        }
    }
}
