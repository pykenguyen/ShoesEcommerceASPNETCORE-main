using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Models;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productService;

        public HomeController(ILogger<HomeController> logger, IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured product variants for homepage display
                var featuredVariants = await _productService.GetFeaturedProductVariantsAsync(12);
                ViewBag.FeaturedProductVariants = featuredVariants;
                
                // Get hot deals (variants with active discounts)
                var hotDeals = featuredVariants.Where(v => v.HasActiveDiscount).Take(8).ToList();
                ViewBag.HotDeals = hotDeals;
                
                // Get new arrivals (first 4 featured variants)
                var newArrivals = featuredVariants.Take(4).ToList();
                ViewBag.NewArrivals = newArrivals;
                
                // Get categories and brands for dynamic filtering
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();
                ViewBag.Categories = categories;
                ViewBag.Brands = brands;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page data");
                ViewBag.FeaturedProductVariants = new List<object>();
                ViewBag.HotDeals = new List<object>();
                ViewBag.NewArrivals = new List<object>();
                ViewBag.Categories = new List<object>();
                ViewBag.Brands = new List<object>();
                return View();
            }
        }

        // Static pages
        [Route("gioi-thieu")]
        public IActionResult About()
        {
            ViewData["Title"] = "Giới thiệu về SPORTS";
            ViewData["MetaDescription"] = "SPORTS - Chuyên cung cấp giày thể thao, giày đá bóng, futsal chính hãng. Cam kết 100% hàng chính hãng.";
            return View();
        }

        [Route("cua-hang")]
        public IActionResult Store()
        {
            ViewData["Title"] = "Hệ thống cửa hàng SPORTS";
            ViewData["MetaDescription"] = "Tìm cửa hàng SPORTS gần bạn nhất. Hỗ trợ thử giày, tư vấn chuyên nghiệp.";
            return View();
        }

        [Route("lien-he")]
        public IActionResult Contact()
        {
            ViewData["Title"] = "Liên hệ SPORTS";
            ViewData["MetaDescription"] = "Liên hệ với SPORTS - Hotline: 0939 345 555. Hỗ trợ 24/7.";
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Fixed browser probe handlers - separate routes for each pattern
        [HttpGet("/.well-known/{path}")]
        public IActionResult HandleWellKnown(string path)
        {
            // Return 204 No Content for .well-known requests
            return NoContent();
        }

        [HttpGet("/favicon.ico")]
        public IActionResult HandleFavicon()
        {
            // Redirect to static file if exists
            return PhysicalFile(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "favicon.ico"), "image/x-icon");
        }

        [HttpGet("/{filename}.map")]
        public IActionResult HandleSourceMaps(string filename)
        {
            // Return 204 No Content for source map requests
            return NoContent();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
