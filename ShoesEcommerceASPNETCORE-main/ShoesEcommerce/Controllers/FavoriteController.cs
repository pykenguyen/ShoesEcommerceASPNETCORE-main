using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services.Interfaces;
using System.Security.Claims;

namespace ShoesEcommerce.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(IFavoriteService favoriteService, ILogger<FavoriteController> logger)
        {
            _favoriteService = favoriteService;
            _logger = logger;
        }

        // GET: Favorite/Index - Trang danh sách yêu thích
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Favorite" });
            }

            var favorites = await _favoriteService.GetFavoritesByCustomerIdAsync(customerId);
            ViewData["Title"] = "Danh sách yêu thích";
            return View(favorites);
        }

        // POST: Favorite/Toggle - Toggle favorite (AJAX)
        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0)
            {
                return Json(new { 
                    success = false, 
                    requireLogin = true,
                    message = "Vui lòng ??ng nh?p ?? thêm vào yêu thích" 
                });
            }

            var result = await _favoriteService.ToggleFavoriteAsync(customerId, productId);
            return Json(new { 
                success = result.Success, 
                isFavorite = result.IsFavorite, 
                message = result.Message 
            });
        }

        // GET: Favorite/Check/{productId} - Check if product is favorite (AJAX)
        [HttpGet]
        public async Task<IActionResult> Check(int productId)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0)
            {
                return Json(new { isFavorite = false, isLoggedIn = false });
            }

            var isFavorite = await _favoriteService.IsFavoriteAsync(customerId, productId);
            return Json(new { isFavorite = isFavorite, isLoggedIn = true });
        }

        // GET: Favorite/GetFavoriteIds - Get all favorite product IDs (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetFavoriteIds()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0)
            {
                return Json(new { productIds = new List<int>(), isLoggedIn = false });
            }

            var productIds = await _favoriteService.GetFavoriteProductIdsAsync(customerId);
            return Json(new { productIds = productIds, isLoggedIn = true });
        }

        // POST: Favorite/Remove - Remove from favorites (for Favorite Index page)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Remove(int productId)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            await _favoriteService.RemoveFromFavoriteAsync(customerId, productId);
            TempData["Success"] = "?ã xóa s?n ph?m kh?i danh sách yêu thích";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentCustomerId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return 0;

                if (int.TryParse(userIdClaim, out int customerId))
                    return customerId;

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current customer ID");
                return 0;
            }
        }
    }
}
