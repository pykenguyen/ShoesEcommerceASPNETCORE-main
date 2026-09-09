using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Controllers.Admin
{
    /// <summary>
    /// Admin controller for managing email campaigns via Mailchimp
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("Admin/Campaign")]
    public class AdminCampaignController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IProductService _productService;
        private readonly ILogger<AdminCampaignController> _logger;

        public AdminCampaignController(
            IEmailService emailService,
            IProductService productService,
            ILogger<AdminCampaignController> logger)
        {
            _emailService = emailService;
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Campaign management page
        /// </summary>
        [HttpGet("")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Qu?n lý Campaign - Admin";
            return View();
        }

        /// <summary>
        /// Create promotion campaign page
        /// </summary>
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "T?o Campaign M?i";
            
            // Get discounted product variants for the campaign
            var discountedProducts = await _productService.GetDiscountedProductVariantsAsync(1, 8);
            ViewBag.Products = discountedProducts;
            
            return View();
        }

        /// <summary>
        /// Send promotion campaign
        /// </summary>
        [HttpPost("SendPromotion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPromotion(
            string campaignTitle,
            string subject,
            string promoCode,
            int discountPercent,
            DateTime expiryDate,
            int[]? productIds)
        {
            try
            {
                _logger.LogInformation(
                    "Creating promotion campaign: {Title}, Code: {Code}, Discount: {Discount}%",
                    campaignTitle, promoCode, discountPercent);

                // Get featured products if specified
                List<PromotionProduct>? featuredProducts = null;
                if (productIds != null && productIds.Length > 0)
                {
                    featuredProducts = new List<PromotionProduct>();
                    foreach (var productId in productIds.Take(4))
                    {
                        var product = await _productService.GetProductByIdAsync(productId);
                        if (product != null)
                        {
                            var variant = product.Variants?.FirstOrDefault();
                            featuredProducts.Add(new PromotionProduct
                            {
                                Name = product.Name,
                                ImageUrl = variant?.ImageUrl ?? "/images/no-image.svg",
                                OriginalPrice = variant?.Price ?? 0,
                                SalePrice = variant?.Price ?? 0,
                                ProductUrl = $"/san-pham/{product.Slug}"
                            });
                        }
                    }
                }

                // Cast to EmailService to access SendPromotionCampaignAsync
                if (_emailService is EmailService emailService)
                {
                    var result = await emailService.SendPromotionCampaignAsync(
                        campaignTitle,
                        subject,
                        promoCode,
                        discountPercent,
                        expiryDate,
                        featuredProducts);

                    if (result)
                    {
                        TempData["Success"] = $"Campaign \"{campaignTitle}\" ?ã ???c g?i thành công!";
                        _logger.LogInformation("Promotion campaign sent successfully: {Title}", campaignTitle);
                    }
                    else
                    {
                        TempData["Error"] = "Không th? g?i campaign. Vui lòng ki?m tra c?u hình Mailchimp.";
                        _logger.LogWarning("Failed to send promotion campaign: {Title}", campaignTitle);
                    }
                }
                else
                {
                    TempData["Error"] = "Email service không h? tr? campaign.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion campaign");
                TempData["Error"] = $"L?i: {ex.Message}";
                return RedirectToAction("Create");
            }
        }

        /// <summary>
        /// Quick send a flash sale campaign
        /// </summary>
        [HttpPost("QuickFlashSale")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickFlashSale()
        {
            try
            {
                // Get top discounted product variants
                var products = await _productService.GetDiscountedProductVariantsAsync(1, 4);
                
                var featuredProducts = products.Select(p => new PromotionProduct
                {
                    Name = p.ProductName,
                    ImageUrl = p.ImageUrl ?? "/images/no-image.svg",
                    OriginalPrice = p.Price,
                    SalePrice = p.HasActiveDiscount ? p.DiscountedPrice : p.Price,
                    ProductUrl = $"/san-pham"
                }).ToList();

                if (_emailService is EmailService emailService)
                {
                    var result = await emailService.SendPromotionCampaignAsync(
                        "? Flash Sale - Ch? hôm nay!",
                        "? Flash Sale - Gi?m ??n 50% - Ch? hôm nay!",
                        "FLASH50",
                        50,
                        DateTime.Today.AddDays(1),
                        featuredProducts);

                    if (result)
                    {
                        TempData["Success"] = "Flash Sale campaign ?ã ???c g?i!";
                    }
                    else
                    {
                        TempData["Error"] = "Không th? g?i Flash Sale campaign.";
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending flash sale campaign");
                TempData["Error"] = $"L?i: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
