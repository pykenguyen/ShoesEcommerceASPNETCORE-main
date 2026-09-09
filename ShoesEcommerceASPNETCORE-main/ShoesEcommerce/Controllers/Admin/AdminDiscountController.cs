using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShoesEcommerce.Models.Promotions;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.ViewModels.Promotion;

namespace ShoesEcommerce.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminDiscountController : Controller
    {
        private readonly IDiscountService _discountService;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<AdminDiscountController> _logger;

        public AdminDiscountController(
            IDiscountService discountService,
            IProductRepository productRepository,
            ILogger<AdminDiscountController> logger)
        {
            _discountService = discountService;
            _productRepository = productRepository;
            _logger = logger;
        }

        // GET: Admin/AdminDiscount
        public async Task<IActionResult> Index(string? searchTerm, bool? isActive, DiscountType? type, int page = 1)
        {
            try
            {
                ViewData["Title"] = "Qu?n lý Khuy?n mãi";
                
                const int pageSize = 10;
                var model = await _discountService.GetDiscountsAsync(searchTerm, isActive, type, page, pageSize);
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading discount list");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i danh sách khuy?n mãi.";
                return View(new DiscountListViewModel());
            }
        }

        // GET: Admin/AdminDiscount/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                ViewData["Title"] = "Chi ti?t Khuy?n mãi";
                
                var discount = await _discountService.GetDiscountByIdAsync(id);
                if (discount == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y khuy?n mãi!";
                    return RedirectToAction(nameof(Index));
                }

                // Get statistics
                var statistics = await _discountService.GetDiscountStatisticsAsync(id);
                
                // Get associated products and categories
                var products = await _discountService.GetDiscountProductsAsync(id);
                var categories = await _discountService.GetDiscountCategoriesAsync(id);

                ViewBag.Statistics = statistics;
                ViewBag.Products = products;
                ViewBag.Categories = categories;

                return View(discount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading discount details for ID: {DiscountId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i chi ti?t khuy?n mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/AdminDiscount/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewData["Title"] = "T?o Khuy?n mãi m?i";
                
                await LoadDropdownDataAsync();
                
                return View(new CreateDiscountViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create discount page");
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i trang t?o khuy?n mãi.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/AdminDiscount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDiscountViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDropdownDataAsync();
                    return View(model);
                }

                // Validate discount code uniqueness
                if (!await _discountService.IsDiscountCodeUniqueAsync(model.Code))
                {
                    ModelState.AddModelError("Code", "Mã khuy?n mãi ?ã t?n t?i.");
                    await LoadDropdownDataAsync();
                    return View(model);
                }

                // Validate discount data
                if (!await _discountService.ValidateDiscountDataAsync(model))
                {
                    ModelState.AddModelError("", "D? li?u khuy?n mãi không h?p l?.");
                    await LoadDropdownDataAsync();
                    return View(model);
                }

                var discountInfo = await _discountService.CreateDiscountAsync(model);
                
                TempData["SuccessMessage"] = $"T?o khuy?n mãi '{discountInfo.Name}' thành công!";
                return RedirectToAction(nameof(Details), new { id = discountInfo.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating discount");
                ModelState.AddModelError("", "Có l?i x?y ra khi t?o khuy?n mãi. Vui lòng th? l?i.");
                await LoadDropdownDataAsync();
                return View(model);
            }
        }

        // GET: Admin/AdminDiscount/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewData["Title"] = "Ch?nh s?a Khuy?n mãi";
                
                var discount = await _discountService.GetDiscountByIdAsync(id);
                if (discount == null)
                {
                    TempData["ErrorMessage"] = "Không tìm th?y khuy?n mãi!";
                    return RedirectToAction(nameof(Index));
                }

                // Get associated products and categories
                var products = await _discountService.GetDiscountProductsAsync(id);
                var categories = await _discountService.GetDiscountCategoriesAsync(id);

                var model = new EditDiscountViewModel
                {
                    Id = discount.Id,
                    Name = discount.Name,
                    Description = discount.Description,
                    Code = discount.Code,
                    Type = discount.Type,
                    PercentageValue = discount.PercentageValue,
                    FixedValue = discount.FixedValue,
                    MinimumOrderValue = discount.MinimumOrderValue,
                    MaximumDiscountAmount = discount.MaximumDiscountAmount,
                    StartDate = discount.StartDate,
                    EndDate = discount.EndDate,
                    IsActive = discount.IsActive,
                    IsFeatured = discount.IsFeatured,
                    MaxUsageCount = discount.MaxUsageCount,
                    MaxUsagePerCustomer = discount.MaxUsagePerCustomer,
                    Scope = discount.Scope,
                    CurrentUsageCount = discount.CurrentUsageCount,
                    SelectedProductIds = products.Select(p => p.Id).ToList(),
                    SelectedCategoryIds = categories.Select(c => c.Id).ToList()
                };

                await LoadDropdownDataAsync();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit discount page for ID: {DiscountId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i trang ch?nh s?a.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/AdminDiscount/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditDiscountViewModel model)
        {
            try
            {
                if (id != model.Id)
                {
                    TempData["ErrorMessage"] = "D? li?u không h?p l?.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    await LoadDropdownDataAsync();
                    return View(model);
                }

                // Validate discount code uniqueness (excluding current discount)
                if (!await _discountService.IsDiscountCodeUniqueAsync(model.Code, id))
                {
                    ModelState.AddModelError("Code", "Mã khuy?n mãi ?ã t?n t?i.");
                    await LoadDropdownDataAsync();
                    return View(model);
                }

                var success = await _discountService.UpdateDiscountAsync(id, model);
                if (!success)
                {
                    ModelState.AddModelError("", "Không th? c?p nh?t khuy?n mãi.");
                    await LoadDropdownDataAsync();
                    return View(model);
                }

                TempData["SuccessMessage"] = "C?p nh?t khuy?n mãi thành công!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discount with ID: {DiscountId}", id);
                ModelState.AddModelError("", "Có l?i x?y ra khi c?p nh?t khuy?n mãi.");
                await LoadDropdownDataAsync();
                return View(model);
            }
        }

        // POST: Admin/AdminDiscount/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!await _discountService.CanDeleteDiscountAsync(id))
                {
                    TempData["ErrorMessage"] = "Không th? xóa khuy?n mãi ?ã ???c s? d?ng.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var success = await _discountService.DeleteDiscountAsync(id);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Không th? xóa khuy?n mãi.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["SuccessMessage"] = "Xóa khuy?n mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting discount with ID: {DiscountId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi xóa khuy?n mãi.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Admin/AdminDiscount/Statistics/5
        public async Task<IActionResult> Statistics(int id)
        {
            try
            {
                ViewData["Title"] = "Th?ng kê Khuy?n mãi";
                
                var statistics = await _discountService.GetDiscountStatisticsAsync(id);
                var usageHistory = await _discountService.GetDiscountUsageHistoryAsync(id);

                ViewBag.UsageHistory = usageHistory;
                
                return View(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading discount statistics for ID: {DiscountId}", id);
                TempData["ErrorMessage"] = "Có l?i x?y ra khi t?i th?ng kê.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // Helper method to load dropdown data
        private async Task LoadDropdownDataAsync()
        {
            try
            {
                // Load products for dropdown
                var products = await _productRepository.GetAllProductsAsync();
                ViewBag.Products = new SelectList(products, "Id", "Name");

                // Load categories for dropdown
                var categories = await _productRepository.GetAllCategoriesAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");

                // Discount types
                ViewBag.DiscountTypes = new SelectList(Enum.GetValues(typeof(DiscountType)));

                // Discount scopes
                ViewBag.DiscountScopes = new SelectList(Enum.GetValues(typeof(DiscountScope)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdown data");
            }
        }
    }
}
