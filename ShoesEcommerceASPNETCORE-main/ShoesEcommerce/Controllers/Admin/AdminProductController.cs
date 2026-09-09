using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Controllers.Admin
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly IDiscountService _discountService;
        private readonly ILogger<AdminProductController> _logger;

        public AdminProductController(IProductService productService, IStockService stockService, IDiscountService discountService, ILogger<AdminProductController> logger)
        {
            _productService = productService;
            _stockService = stockService;
            _discountService = discountService;
            _logger = logger;
        }

        // GET: Admin/Product
        public async Task<IActionResult> Index(string searchTerm, int? categoryId, int? brandId, int page = 1, int pageSize = 10)
        {
            ViewData["Title"] = "Quản lý Sản phẩm";

            try
            {
                var viewModel = await _productService.GetProductsAsync(searchTerm, categoryId, brandId, page, pageSize);

                // Get data for dropdowns
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", brandId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product index page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách sản phẩm: " + ex.Message;
                
                var emptyViewModel = new ProductListViewModel();
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
                return View(emptyViewModel);
            }
        }

        // GET: Admin/Product/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi tiết Sản phẩm";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                var variants = await _productService.GetProductVariantsAsync(id);
                ViewBag.Variants = variants;

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Product/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Thêm Sản phẩm mới";

            try
            {
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                ViewBag.Brands = new SelectList(brands, "Id", "Name");

                return View(new CreateProductViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product create page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form thêm sản phẩm: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            ViewData["Title"] = "Thêm Sản phẩm mới";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdProduct = await _productService.CreateProductAsync(model);
                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Details), new { id = createdProduct.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
                }
            }

            // Reload dropdowns if validation fails
            try
            {
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", model.BrandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdowns for create form");
                ModelState.AddModelError("", "Không thể tải danh sách: " + ex.Message);
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
            }

            return View(model);
        }

        // GET: Admin/Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Sản phẩm";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                var model = new EditProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    CategoryId = product.CategoryId,
                    BrandId = product.BrandId
                };

                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", model.BrandId);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product edit page for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải form chỉnh sửa: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditProductViewModel model)
        {
            ViewData["Title"] = "Chỉnh sửa Sản phẩm";

            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _productService.UpdateProductAsync(id, model);
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Không thể cập nhật sản phẩm. Vui lòng thử lại.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product with ID: {ProductId}", id);
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
                }
            }

            // Reload dropdowns if validation fails
            try
            {
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                ViewBag.Categories = new SelectList(categories, "Id", "Name", model.CategoryId);
                ViewBag.Brands = new SelectList(brands, "Id", "Name", model.BrandId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dropdowns for edit form");
                ViewBag.Categories = new SelectList(new List<object>(), "Id", "Name");
                ViewBag.Brands = new SelectList(new List<object>(), "Id", "Name");
            }

            return View(model);
        }

        // GET: Admin/Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            ViewData["Title"] = "Xóa Sản phẩm";

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction(nameof(Index));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product delete page for ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // === CATEGORIES ===

        // GET: Admin/Product/Categories
        public async Task<IActionResult> Categories()
        {
            ViewData["Title"] = "Quản lý Danh mục";

            try
            {
                var categories = await _productService.GetAllCategoriesAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách danh mục: " + ex.Message;
                return View(new List<CategoryInfo>());
            }
        }

        // GET: Admin/Product/CreateCategory
        public IActionResult CreateCategory()
        {
            ViewData["Title"] = "Thêm Danh mục mới";
            return View(new CreateCategoryViewModel());
        }

        // POST: Admin/Product/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CreateCategoryViewModel model)
        {
            ViewData["Title"] = "Thêm Danh mục mới";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdCategory = await _productService.CreateCategoryAsync(model);
                    TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Categories));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm danh mục: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Admin/Product/EditCategory/5
        public async Task<IActionResult> EditCategory(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Danh mục";
            
            try
            {
                var category = await _productService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                    return RedirectToAction(nameof(Categories));
                }
                
                var model = new EditCategoryViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    Description = category.Description
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category for edit: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Categories));
            }
        }

        // POST: Admin/Product/EditCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, EditCategoryViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ!";
                return RedirectToAction(nameof(Categories));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var success = await _productService.UpdateCategoryAsync(id, model);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                        return RedirectToAction(nameof(Categories));
                    }
                    ModelState.AddModelError("", "Không thể cập nhật danh mục.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating category: {Id}", id);
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Admin/Product/DeleteCategory/5
        public async Task<IActionResult> DeleteCategory(int id)
        {
            ViewData["Title"] = "Xóa Danh mục";
            
            try
            {
                var category = await _productService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                    return RedirectToAction(nameof(Categories));
                }
                
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category for delete: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Categories));
            }
        }

        // POST: Admin/Product/DeleteCategory/5
        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoryConfirmed(int id)
        {
            try
            {
                var success = await _productService.DeleteCategoryAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Xóa danh mục thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa danh mục. Có thể danh mục đang được sử dụng.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message;
            }

            return RedirectToAction(nameof(Categories));
        }

        // === BRANDS ===

        // GET: Admin/Product/Brands
        public async Task<IActionResult> Brands()
        {
            ViewData["Title"] = "Quản lý Thương hiệu";

            try
            {
                var brands = await _productService.GetAllBrandsAsync();
                return View(brands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading brands page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách thương hiệu: " + ex.Message;
                return View(new List<BrandInfo>());
            }
        }

        // GET: Admin/Product/CreateBrand
        public IActionResult CreateBrand()
        {
            ViewData["Title"] = "Thêm Thương hiệu mới";
            return View(new CreateBrandViewModel());
        }

        // POST: Admin/Product/CreateBrand
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBrand(CreateBrandViewModel model)
        {
            ViewData["Title"] = "Thêm Thương hiệu mới";

            if (ModelState.IsValid)
            {
                try
                {
                    var createdBrand = await _productService.CreateBrandAsync(model);
                    TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                    return RedirectToAction(nameof(Brands));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating brand");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm thương hiệu: " + ex.Message);
                }
            }

            return View(model);
        }

        // ===== SUPPLIER MANAGEMENT =====
        [HttpGet]
        public async Task<IActionResult> Suppliers()
        {
            ViewData["Title"] = "Quản lý Nhà cung cấp";
            
            try
            {
                var suppliers = await _productService.GetAllSuppliersAsync();
                return View(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading suppliers");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách nhà cung cấp";
                return View(new List<SupplierInfo>());
            }
        }

        [HttpGet]
        public IActionResult CreateSupplier()
        {
            ViewData["Title"] = "Thêm Nhà cung cấp mới";
            return View(new CreateSupplierViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(CreateSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var supplier = await _productService.CreateSupplierAsync(model);
                TempData["SuccessMessage"] = $"Nhà cung cấp '{supplier.Name}' đã được tạo thành công";
                return RedirectToAction(nameof(Suppliers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier: {Name}", model.Name);
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo nhà cung cấp: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSupplier(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Nhà cung cấp";
            
            try
            {
                var supplier = await _productService.GetSupplierByIdAsync(id);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp";
                    return RedirectToAction(nameof(Suppliers));
                }

                var model = new EditSupplierViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactInfo = supplier.ContactInfo
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier for edit: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin nhà cung cấp";
                return RedirectToAction(nameof(Suppliers));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupplier(int id, EditSupplierViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _productService.UpdateSupplierAsync(id, model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công";
                    return RedirectToAction(nameof(Suppliers));
                }
                else
                {
                    ModelState.AddModelError("", "Không thể cập nhật nhà cung cấp");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier: {Id}", id);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật nhà cung cấp: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            try
            {
                var success = await _productService.DeleteSupplierAsync(id);
                if (success)
                {
                    TempData["SuccessMessage"] = "Xóa nhà cung cấp thành công";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier: {Id}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa nhà cung cấp: " + ex.Message;
            }

            return RedirectToAction(nameof(Suppliers));
        }

        // ===== HELPER METHODS =====

        // GET: Admin/Product/CreateVariant
        [HttpGet]
        public async Task<IActionResult> CreateVariant(int productId)
        {
            ViewData["Title"] = "Thêm Phiên bản mới";
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Product = product;
            var model = new CreateProductVariantViewModel { ProductId = productId };
            return View(model);
        }

        // POST: Admin/Product/CreateVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVariant(CreateProductVariantViewModel model)
        {
            ViewData["Title"] = "Thêm Phiên bản mới";
            var product = await _productService.GetProductByIdAsync(model.ProductId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Product = product;
            if (ModelState.IsValid)
            {
                try
                {
                    var createdVariant = await _productService.CreateProductVariantAsync(model);
                    TempData["SuccessMessage"] = "Thêm phiên bản thành công!";
                    return RedirectToAction("Details", new { id = model.ProductId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product variant");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi thêm phiên bản: " + ex.Message);
                }
            }
            return View(model);
        }

        // GET: Admin/Product/Variants
        public async Task<IActionResult> Variants(int productId)
        {
            ViewData["Title"] = "Phiên bản sản phẩm";
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Product = product;
            var variants = await _productService.GetProductVariantsAsync(productId);
            return View(variants);
        }

        // GET: Admin/Product/DiscountedProducts
        public async Task<IActionResult> DiscountedProducts(int page = 1, int pageSize = 12)
        {
            ViewData["Title"] = "Sản phẩm khuyến mãi";
            try
            {
                var discountedVariants = await _productService.GetDiscountedProductVariantsAsync(page, pageSize);
                var featuredDiscounts = await _discountService.GetFeaturedDiscountsAsync();
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

        // GET: Admin/Product/Search
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

        // GET: Admin/Product/GetByCategory
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

        // GET: Admin/Product/GetByBrand
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

        // GET: Admin/Product/GetVariants
        [HttpGet]
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

        // GET: Admin/Product/GetDiscountInfo
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
                var result = new {
                    hasDiscount = discountInfo.HasActiveDiscount,
                    discountName = discountInfo.ActiveDiscount?.Name,
                    discountCode = discountInfo.ActiveDiscount?.Code,
                    discountPercentage = discountInfo.DiscountPercentage,
                    discountAmount = discountInfo.DiscountAmount,
                    originalPrice = discountInfo.OriginalPrice,
                    discountedPrice = discountInfo.DiscountedPrice
                };
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount info for product: {ProductId}", productId);
                return Json(new { hasDiscount = false });
            }
        }

        // GET: Admin/Product/EditVariant/5
        [HttpGet]
        public async Task<IActionResult> EditVariant(int id)
        {
            ViewData["Title"] = "Chỉnh sửa Phiên bản";
            var variant = await _productService.GetProductVariantByIdAsync(id);
            if (variant == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên bản!";
                return RedirectToAction("Variants", new { productId = variant?.ProductId });
            }
            var product = await _productService.GetProductByIdAsync(variant.ProductId);
            ViewBag.Product = product;
            var model = new EditProductVariantViewModel
            {
                Id = variant.Id,
                ProductId = variant.ProductId,
                Color = variant.Color,
                Size = variant.Size,
                Price = variant.Price,
                ImageUrl = variant.ImageUrl
            };
            return View(model);
        }

        // POST: Admin/Product/EditVariant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVariant(EditProductVariantViewModel model)
        {
            _logger.LogInformation("=== EditVariant POST ===");
            _logger.LogInformation("Model: Id={Id}, ProductId={ProductId}, Color={Color}, Size={Size}, Price={Price}", 
                model.Id, model.ProductId, model.Color, model.Size, model.Price);
            _logger.LogInformation("ImageFile: {HasFile}", model.ImageFile != null ? $"Yes ({model.ImageFile.FileName}, {model.ImageFile.Length} bytes)" : "No");
            _logger.LogInformation("KeepCurrentImage: {Keep}, UseImageUrl: {UseUrl}, ImageUrl: {Url}", 
                model.KeepCurrentImage, model.UseImageUrl, model.ImageUrl ?? "(null)");
            
            ViewData["Title"] = "Chỉnh sửa Phiên bản";
            var product = await _productService.GetProductByIdAsync(model.ProductId);
            ViewBag.Product = product;
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is INVALID. Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state?.Errors?.Count > 0)
                    {
                        foreach (var error in state.Errors)
                        {
                            _logger.LogWarning("  - {Key}: {Error}", key, error.ErrorMessage);
                        }
                    }
                }
                return View(model);
            }
            
            _logger.LogInformation("ModelState is VALID. Calling UpdateProductVariantAsync...");
            var result = await _productService.UpdateProductVariantAsync(model.Id, model);
            if (result)
            {
                _logger.LogInformation("Update SUCCESS!");
                TempData["SuccessMessage"] = "Cập nhật phiên bản thành công!";
                return RedirectToAction("Details", new { id = model.ProductId });
            }
            
            _logger.LogWarning("Update FAILED!");
            ModelState.AddModelError("", "Không thể cập nhật phiên bản. Vui lòng thử lại.");
            return View(model);
        }

        // GET: Admin/Product/DeleteVariant/5
        [HttpGet]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            ViewData["Title"] = "Xóa Phiên bản";
            
            var variant = await _productService.GetProductVariantByIdAsync(id);
            if (variant == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên bản!";
                return RedirectToAction(nameof(Index));
            }
            
            var product = await _productService.GetProductByIdAsync(variant.ProductId);
            ViewBag.Product = product;
            ViewBag.Variant = variant;
            
            return View(variant);
        }

        // POST: Admin/Product/DeleteVariant/5
        [HttpPost, ActionName("DeleteVariant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariantConfirmed(int id)
        {
            var variant = await _productService.GetProductVariantByIdAsync(id);
            var productId = variant?.ProductId ?? 0;
            
            try
            {
                var result = await _productService.DeleteProductVariantAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Xóa phiên bản thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa phiên bản. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product variant with ID: {VariantId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa phiên bản: " + ex.Message;
            }

            if (productId > 0)
            {
                return RedirectToAction("Details", new { id = productId });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}