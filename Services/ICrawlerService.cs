using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler.Services
{
    public interface ICrawlerService
    {
        Task CrawlAsync();
    }
}
