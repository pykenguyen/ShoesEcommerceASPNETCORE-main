using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Controllers
{
    public class DiscountController : Controller
    {
        private readonly IDiscountService _discountService;
        private readonly ILogger<DiscountController> _logger;

        public DiscountController(
            IDiscountService discountService,
            ILogger<DiscountController> logger)
        {
            _discountService = discountService;
            _logger = logger;
        }

        // GET: Discount (List all active discounts)
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["Title"] = "Khuy?n mãi";
                
                var activeDiscounts = await _discountService.GetActiveDiscountsAsync();
                var featuredDiscounts = await _discountService.GetFeaturedDiscountsAsync(10);

                ViewBag.FeaturedDiscounts = featuredDiscounts;
                
                return View(activeDiscounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading discounts page");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i trang khuy?n mãi.";
                return View(new List<ViewModels.Promotion.DiscountInfo>());
            }
        }

        // GET: Discount/Products (Products with discounts)
        public async Task<IActionResult> Products(int page = 1)
        {
            try
            {
                ViewData["Title"] = "S?n ph?m khuy?n mãi";
                
                const int pageSize = 12;
                var products = await _discountService.GetDiscountedProductsAsync(page, pageSize);
                
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading discounted products");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i s?n ph?m khuy?n mãi.";
                return View(new List<ViewModels.Product.ProductInfo>());
            }
        }

        // POST: Discount/ValidateCode (AJAX endpoint for validating discount codes)
        [HttpPost]
        public async Task<IActionResult> ValidateCode(string code, decimal orderValue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { success = false, message = "Vui lòng nh?p mã khuy?n mãi." });
                }

                var customerEmail = User.Identity?.Name ?? "guest@temp.com";
                
                var canUse = await _discountService.CanUseDiscountAsync(code.ToUpper(), customerEmail, orderValue);
                
                if (!canUse)
                {
                    return Json(new { success = false, message = "Mã khuy?n mãi không h?p l? ho?c không th? s? d?ng." });
                }

                var result = await _discountService.ApplyDiscountAsync(code.ToUpper(), customerEmail, orderValue);
                
                if (!result.IsSuccessful)
                {
                    return Json(new { success = false, message = result.Message });
                }

                return Json(new
                {
                    success = true,
                    message = result.Message,
                    discountAmount = result.DiscountAmount,
                    finalPrice = result.FinalPrice,
                    discountCode = code.ToUpper(),
                    discountId = result.Discount?.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating discount code: {Code}", code);
                return Json(new { success = false, message = "Có l?i x?y ra khi ki?m tra mã khuy?n mãi." });
            }
        }
    }
}
