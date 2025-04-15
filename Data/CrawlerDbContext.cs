using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebCrawler.Models;

namespace WebCrawler.Data
{
    public class CrawlerDbContext : DbContext
    {
        public DbSet<CrawledPage> CrawledPages { get; set; } = null!;

        public CrawlerDbContext(DbContextOptions<CrawlerDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CrawledPage>().HasKey(p => p.Id);
        }
    }
}
