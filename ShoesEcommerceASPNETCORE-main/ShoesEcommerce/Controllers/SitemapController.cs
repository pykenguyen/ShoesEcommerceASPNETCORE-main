using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Helpers;
using System.Text;
using System.Xml.Linq;

namespace ShoesEcommerce.Controllers
{
    /// <summary>
    /// Controller for generating XML sitemap for SEO
    /// </summary>
    public class SitemapController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public SitemapController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Generates XML sitemap for search engine crawlers
        /// </summary>
        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<IActionResult> Index()
        {
            var baseUrl = GetBaseUrl();
            
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            
            var sitemap = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "urlset",
                    // Static pages
                    CreateUrlElement(ns, baseUrl, "/", "1.0", "daily"),
                    CreateUrlElement(ns, baseUrl, "/san-pham", "0.9", "daily"),
                    CreateUrlElement(ns, baseUrl, "/khuyen-mai", "0.8", "daily"),
                    CreateUrlElement(ns, baseUrl, "/dang-nhap", "0.5", "monthly"),
                    CreateUrlElement(ns, baseUrl, "/dang-ky", "0.5", "monthly")
                )
            );

            // Add product pages dynamically
            var products = await _context.Products
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            foreach (var product in products)
            {
                var slug = product.Name.ToSlugWithId(product.Id);
                var lastMod = DateTime.UtcNow.ToString("yyyy-MM-dd");
                
                sitemap.Root?.Add(
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/san-pham/{slug}"),
                        new XElement(ns + "lastmod", lastMod),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.8")
                    )
                );
            }

            // Add category pages
            var categories = await _context.Categories
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            foreach (var category in categories)
            {
                sitemap.Root?.Add(
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/san-pham?category={category.Id}"),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.7")
                    )
                );
            }

            // Add brand pages
            var brands = await _context.Brands
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();

            foreach (var brand in brands)
            {
                sitemap.Root?.Add(
                    new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}/san-pham?brand={brand.Id}"),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.7")
                    )
                );
            }

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                sitemap.Save(writer);
            }

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }

        private string GetBaseUrl()
        {
            // Try to get from configuration first
            var configuredUrl = _configuration["SiteUrl"];
            if (!string.IsNullOrEmpty(configuredUrl))
                return configuredUrl.TrimEnd('/');

            // Fallback to request URL
            return $"{Request.Scheme}://{Request.Host}";
        }

        private static XElement CreateUrlElement(XNamespace ns, string baseUrl, string path, string priority, string changeFreq)
        {
            return new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}{path}"),
                new XElement(ns + "changefreq", changeFreq),
                new XElement(ns + "priority", priority)
            );
        }
    }
}
