using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;
using ShoesEcommerce.Helpers;

namespace ShoesEcommerce.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly ICommentService _commentService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IDiscountService discountService,
            ICommentService commentService,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _discountService = discountService;
            _commentService = commentService;
            _logger = logger;
        }

        // GET: Product - NOW DISPLAYS PRODUCT VARIANTS INSTEAD OF PRODUCTS
        // SEO-friendly URL: /san-pham
        [Route("san-pham")]
        [Route("[controller]")]
        [Route("[controller]/[action]")]
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? brandId, int page = 1, int pageSize = 12)
        {
            ViewData["Title"] = "Danh sách sản phẩm";

            try
            {
                var model = await _productService.GetProductVariantsListAsync(searchString, categoryId, brandId, page, pageSize);
                
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();
                
                ViewBag.Categories = categories.Select(c => new { c.Id, c.Name }).ToList();
                ViewBag.Brands = brands.Select(b => new { b.Id, b.Name }).ToList();

                ViewData["CurrentFilter"] = searchString;
                ViewData["CategoryFilter"] = categoryId;
                ViewData["BrandFilter"] = brandId;

                var featuredDiscounts = await _discountService.GetFeaturedDiscountsAsync();
                model.FeaturedDiscounts = featuredDiscounts;

                return View("VariantIndex", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for user page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách sản phẩm.";
                
                var emptyModel = new ProductVariantListViewModel
                {
                    ProductVariants = new List<ProductVariantDisplayInfo>(),
                    CurrentPage = page,
                    TotalPages = 0,
                    TotalItems = 0,
                    SearchTerm = searchString
                };
                
                ViewBag.Categories = new List<object>();
                ViewBag.Brands = new List<object>();
                
                return View("VariantIndex", emptyModel);
            }
        }

        // ==================== SEO-FRIENDLY PRODUCT DETAIL ROUTES ====================
        
        // NEW: Category-based SEO route: /{category-slug}/{product-slug}
        // Example: /giay-da-bong/nike-mercurial-vapor-15-do-28
        [Route("{categorySlug}/{productSlug}")]
        public async Task<IActionResult> CategoryProduct(string categorySlug, string productSlug)
        {
            // Extract product ID from slug
            var productId = SlugHelper.ExtractIdFromSlug(productSlug);
            if (productId <= 0)
            {
                return NotFound();
            }

            return await RenderProductDetails(productId, categorySlug, productSlug);
        }

        // Legacy route: /san-pham/{slug} (keep for backward compatibility)
        [Route("san-pham/{slug}")]
        [Route("product/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var productId = SlugHelper.ExtractIdFromSlug(slug);
            if (productId <= 0)
            {
                return NotFound();
            }

            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return NotFound("Sản phẩm không tồn tại.");
                }

                // Check if this is a social crawler - don't redirect, serve content directly
                var isSocialCrawler = HttpContext.Items.ContainsKey("IsSocialCrawler") && 
                                      (bool)HttpContext.Items["IsSocialCrawler"]!;
                
                if (isSocialCrawler)
                {
                    // Serve content directly to social crawlers without redirect
                    return await RenderProductDetailsForCrawler(product);
                }

                // Redirect to new SEO-friendly URL with category for regular users
                var categorySlug = product.Category?.Name?.ToSlug() ?? "san-pham";
                var newSlug = $"{product.Name.ToSlug()}-{product.Id}";
                var newUrl = $"/{categorySlug}/{newSlug}";

                return RedirectPermanent(newUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redirecting product: {ProductId}", productId);
                return NotFound();
            }
        }

        // Legacy route: /Product/Details/{id}
        [Route("Product/Details/{id:int}")]
        public async Task<IActionResult> DetailsById(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound("Sản phẩm không tồn tại.");
                }

                // Redirect to new SEO-friendly URL with category
                var categorySlug = product.Category?.Name?.ToSlug() ?? "san-pham";
                var newSlug = $"{product.Name.ToSlug()}-{product.Id}";
                var newUrl = $"/{categorySlug}/{newSlug}";

                return RedirectPermanent(newUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redirecting product: {ProductId}", id);
                return NotFound();
            }
        }

        /// <summary>
        /// Core method to render product details
        /// </summary>
        private async Task<IActionResult> RenderProductDetails(int productId, string? categorySlug = null, string? productSlug = null)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    return NotFound("Sản phẩm không tồn tại.");
                }

                // Generate canonical SEO URL (WITHOUT query string)
                var expectedCategorySlug = product.Category?.Name?.ToSlug() ?? "san-pham";
                var expectedProductSlug = $"{product.Name.ToSlug()}-{product.Id}";
                var canonicalUrl = $"/{expectedCategorySlug}/{expectedProductSlug}";

                // Check if this is a social crawler - skip redirect for crawlers
                var isSocialCrawler = HttpContext.Items.ContainsKey("IsSocialCrawler") && 
                                      (bool)HttpContext.Items["IsSocialCrawler"]!;

                // Check if current URL matches canonical URL, redirect if not (but NOT for social crawlers)
                var currentPath = Request.Path.Value?.ToLowerInvariant() ?? "";
                if (!isSocialCrawler && !currentPath.Equals(canonicalUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectPermanent(canonicalUrl);
                }

                // Load product data
                var variants = await _productService.GetProductVariantsAsync(productId);
                var discountInfo = await _discountService.GetProductDiscountInfoAsync(productId);
                var comments = await _commentService.GetCommentsAsync(productId);
                var qas = await _commentService.GetQAsAsync(productId);

                // SEO Meta tags
                var categoryName = product.Category?.Name ?? "Sản phẩm";
                ViewData["Title"] = $"{product.Name} | {categoryName} - SPORTS Vietnam";
                ViewData["MetaDescription"] = product.Description?.Length > 160 
                    ? product.Description.Substring(0, 157) + "..." 
                    : product.Description ?? $"{product.Name} chính hãng tại SPORTS Vietnam";
                
                // IMPORTANT: Canonical URL WITHOUT query string for OG tags
                ViewData["CanonicalUrl"] = $"{Request.Scheme}://{Request.Host}{canonicalUrl}";
                ViewData["OgType"] = "product";
                
                // Set product image for OG
                if (variants.Any())
                {
                    var mainImage = variants.FirstOrDefault()?.ImageUrl;
                    if (!string.IsNullOrEmpty(mainImage))
                    {
                        ViewData["OgImage"] = mainImage.StartsWith("http") 
                            ? mainImage 
                            : $"{Request.Scheme}://{Request.Host}{mainImage}";
                    }
                }
                
                ViewBag.Variants = variants;
                ViewBag.DiscountInfo = discountInfo;
                ViewBag.Comments = comments;
                ViewBag.QAs = qas;
                ViewBag.ProductSlug = expectedProductSlug;
                ViewBag.CategorySlug = expectedCategorySlug;
                ViewBag.CanonicalUrl = canonicalUrl;
                ViewBag.IsSocialCrawler = isSocialCrawler;

                if (isSocialCrawler)
                {
                    _logger.LogInformation("🤖 Serving product to social crawler: {ProductName} (ID: {ProductId})", 
                        product.Name, product.Id);
                }

                return View("Details", product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product details for ID: {ProductId}", productId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: AddComment - Redirect to product index when accessed directly
        [HttpGet]
        public IActionResult AddComment()
        {
            TempData["Info"] = "Vui lòng sử dụng form bình luận trên trang sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(ProductCommentViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Bạn cần đăng nhập để bình luận.";
                return await RedirectToProductDetailsAsync(model.ProductId);
            }
            
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(customerIdClaim, out var customerId))
            {
                model.CustomerId = customerId;
            }
            model.CustomerName = User.Identity.Name ?? "Khách hàng";
            
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return await RedirectToProductDetailsAsync(model.ProductId);
            }
            
            await _commentService.AddCommentAsync(model);
            TempData["Success"] = "Đã gửi bình luận thành công.";
            return await RedirectToProductDetailsAsync(model.ProductId);
        }

        // GET: AddQA - Redirect to product index when accessed directly
        [HttpGet]
        public IActionResult AddQA()
        {
            TempData["Info"] = "Vui lòng sử dụng form hỏi đáp trên trang sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQA(ProductQAViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Bạn cần đăng nhập để gửi câu hỏi.";
                return await RedirectToProductDetailsAsync(model.ProductId);
            }
            
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(customerIdClaim, out var customerId))
            {
                model.CustomerId = customerId;
            }
            model.CustomerName = User.Identity.Name ?? "Khách hàng";
            
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return await RedirectToProductDetailsAsync(model.ProductId);
            }
            
            await _commentService.AddQAAsync(model);
            TempData["Success"] = "Đã gửi câu hỏi thành công.";
            return await RedirectToProductDetailsAsync(model.ProductId);
        }

        // GET: Product/DiscountedProducts - SEO-friendly URL
        [Route("khuyen-mai")]
        [Route("discounted-products")]
        [Route("Product/DiscountedProducts")]
        public async Task<IActionResult> DiscountedProducts(int page = 1, int pageSize = 12)
        {
            ViewData["Title"] = "Sản phẩm khuyến mãi";
            ViewData["MetaDescription"] = "Khám phá các sản phẩm giày dép khuyến mãi với giá ưu đãi tốt nhất. Mua ngay!";

            try
            {
                var discountedVariants = await _productService.GetDiscountedProductVariantsAsync(page, pageSize);
                var featuredDiscounts = await _discountService.GetFeaturedDiscountsAsync();
                
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();
                
                ViewBag.Categories = categories.Select(c => new { c.Id, c.Name }).ToList();
                ViewBag.Brands = brands.Select(b => new { b.Id, b.Name }).ToList();

                var model = new ProductVariantListViewModel
                {
                    ProductVariants = discountedVariants,
                    FeaturedDiscounts = featuredDiscounts,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)discountedVariants.Count() / pageSize),
                    TotalItems = discountedVariants.Count(),
                    ShowDiscountsOnly = true
                };

                return View("VariantIndex", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discounted product variants");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải sản phẩm khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // API endpoint for AJAX calls (for search autocomplete, etc.)
        [HttpGet]
        public async Task<IActionResult> Search(string term, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            try
            {
                var searchResult = await _productService.GetProductVariantsListAsync(term, null, null, 1, limit);
                
                var variants = searchResult.ProductVariants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    name = v.DisplayName,
                    slug = v.ProductName.ToSlugWithId(v.ProductId),
                    // NEW: Include category slug for SEO-friendly URL
                    categorySlug = v.CategoryName.ToSlug(),
                    url = SlugHelper.ToFullProductUrl(v.ProductName, v.CategoryName, null, v.ProductId),
                    price = v.Price,
                    discountedPrice = v.DiscountedPrice,
                    imageUrl = v.ImageUrl,
                    categoryName = v.CategoryName,
                    brandName = v.BrandName,
                    color = v.Color,
                    size = v.Size,
                    hasDiscount = v.HasActiveDiscount,
                    discountPercentage = v.DiscountPercentage,
                    stockQuantity = v.StockQuantity,
                    isInStock = v.IsInStock
                }).ToList();

                return Json(variants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during product variant search for term: {SearchTerm}", term);
                return Json(new List<object>());
            }
        }

        // API endpoint to get variants by category
        [HttpGet]
        public async Task<IActionResult> GetByCategory(int categoryId, int limit = 20)
        {
            try
            {
                var searchResult = await _productService.GetProductVariantsListAsync(null, categoryId, null, 1, limit);
                
                var variants = searchResult.ProductVariants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    name = v.DisplayName,
                    url = SlugHelper.ToFullProductUrl(v.ProductName, v.CategoryName, null, v.ProductId),
                    price = v.Price,
                    discountedPrice = v.DiscountedPrice,
                    imageUrl = v.ImageUrl,
                    brandName = v.BrandName,
                    color = v.Color,
                    size = v.Size,
                    hasDiscount = v.HasActiveDiscount,
                    discountPercentage = v.DiscountPercentage,
                    stockQuantity = v.StockQuantity,
                    isInStock = v.IsInStock
                }).ToList();

                return Json(variants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants by category: {CategoryId}", categoryId);
                return Json(new List<object>());
            }
        }

        // API endpoint to get variants by brand
        [HttpGet]
        public async Task<IActionResult> GetByBrand(int brandId, int limit = 20)
        {
            try
            {
                var searchResult = await _productService.GetProductVariantsListAsync(null, null, brandId, 1, limit);
                
                var variants = searchResult.ProductVariants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    name = v.DisplayName,
                    url = SlugHelper.ToFullProductUrl(v.ProductName, v.CategoryName, null, v.ProductId),
                    price = v.Price,
                    discountedPrice = v.DiscountedPrice,
                    imageUrl = v.ImageUrl,
                    categoryName = v.CategoryName,
                    color = v.Color,
                    size = v.Size,
                    hasDiscount = v.HasActiveDiscount,
                    discountPercentage = v.DiscountPercentage,
                    stockQuantity = v.StockQuantity,
                    isInStock = v.IsInStock
                }).ToList();

                return Json(variants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants by brand: {BrandId}", brandId);
                return Json(new List<object>());
            }
        }

        // API endpoint to get product variants by product ID (for Add to Cart modal)
        [HttpGet]
        [Route("Product/GetVariants/{productId:int}")]
        public async Task<IActionResult> GetVariants(int productId)
        {
            try
            {
                var variants = await _productService.GetProductVariantsAsync(productId);
                
                var variantList = variants.Select(v => new {
                    id = v.Id,
                    productId = v.ProductId,
                    color = v.Color,
                    size = v.Size,
                    price = v.Price,
                    imageUrl = v.ImageUrl,
                    stockQuantity = v.StockQuantity,
                    availableQuantity = v.StockQuantity,
                    isInStock = v.StockQuantity > 0
                }).ToList();

                return Json(variantList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product variants for product: {ProductId}", productId);
                return Json(new List<object>());
            }
        }

        // API endpoint to check discount for a product
        [HttpGet]
        public async Task<IActionResult> GetDiscountInfo(int productId)
        {
            try
            {
                var discountInfo = await _discountService.GetProductDiscountInfoAsync(productId);
                
                if (discountInfo == null)
                {
                    return Json(new { hasDiscount = false });
                }

                return Json(new {
                    hasDiscount = discountInfo.HasActiveDiscount,
                    discountName = discountInfo.ActiveDiscount?.Name,
                    discountCode = discountInfo.ActiveDiscount?.Code,
                    discountPercentage = discountInfo.DiscountPercentage,
                    discountAmount = discountInfo.DiscountAmount,
                    originalPrice = discountInfo.OriginalPrice,
                    discountedPrice = discountInfo.DiscountedPrice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount info for product: {ProductId}", productId);
                return Json(new { hasDiscount = false });
            }
        }

        /// <summary>
        /// Helper method to redirect to product details using SEO-friendly URL
        /// </summary>
        private async Task<IActionResult> RedirectToProductDetailsAsync(int productId)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product != null)
                {
                    var categorySlug = product.Category?.Name?.ToSlug() ?? "san-pham";
                    var productSlug = $"{product.Name.ToSlug()}-{product.Id}";
                    return RedirectPermanent($"/{categorySlug}/{productSlug}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product for redirect: {ProductId}", productId);
            }
            
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Render product details for social crawlers without redirect
        /// </summary>
        private async Task<IActionResult> RenderProductDetailsForCrawler(ShoesEcommerce.Models.Products.Product product)
        {
            try
            {
                // Generate canonical URL without query string
                var categorySlug = product.Category?.Name?.ToSlug() ?? "san-pham";
                var productSlug = $"{product.Name.ToSlug()}-{product.Id}";
                var canonicalUrl = $"/{categorySlug}/{productSlug}";

                // Load product data
                var variants = await _productService.GetProductVariantsAsync(product.Id);
                var discountInfo = await _discountService.GetProductDiscountInfoAsync(product.Id);
                var comments = await _commentService.GetCommentsAsync(product.Id);
                var qas = await _commentService.GetQAsAsync(product.Id);

                // SEO Meta tags - CRITICAL for social sharing
                var categoryName = product.Category?.Name ?? "Sản phẩm";
                ViewData["Title"] = $"{product.Name} | {categoryName} - SPORTS Vietnam";
                ViewData["MetaDescription"] = product.Description?.Length > 160 
                    ? product.Description.Substring(0, 157) + "..." 
                    : product.Description ?? $"{product.Name} chính hãng tại SPORTS Vietnam";
                
                // IMPORTANT: Canonical URL WITHOUT query string for OG tags
                ViewData["CanonicalUrl"] = $"{Request.Scheme}://{Request.Host}{canonicalUrl}";
                ViewData["OgType"] = "product";
                
                ViewBag.Variants = variants;
                ViewBag.DiscountInfo = discountInfo;
                ViewBag.Comments = comments;
                ViewBag.QAs = qas;
                ViewBag.ProductSlug = productSlug;
                ViewBag.CategorySlug = categorySlug;
                ViewBag.CanonicalUrl = canonicalUrl;
                ViewBag.IsSocialCrawler = true;

                _logger.LogInformation("🤖 Serving product to social crawler: {ProductName} (ID: {ProductId})", 
                    product.Name, product.Id);

                return View("Details", product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering product for crawler: {ProductId}", product.Id);
                return NotFound();
            }
        }
    }
}