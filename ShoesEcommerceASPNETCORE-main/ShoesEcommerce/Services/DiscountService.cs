using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Promotions;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.ViewModels.Product;
using ShoesEcommerce.ViewModels.Promotion;

namespace ShoesEcommerce.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly IDiscountRepository _discountRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<DiscountService> _logger;

        public DiscountService(
            IDiscountRepository discountRepository,
            IProductRepository productRepository,
            ILogger<DiscountService> logger)
        {
            _discountRepository = discountRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        // ===== Discount Management =====
        public async Task<DiscountListViewModel> GetDiscountsAsync(string? searchTerm, bool? isActive, DiscountType? type, int page, int pageSize)
        {
            try
            {
                var discounts = await _discountRepository.GetPaginatedDiscountsAsync(page, pageSize, searchTerm, isActive, type);
                var totalCount = await _discountRepository.GetTotalDiscountCountAsync(searchTerm, isActive, type);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var discountInfos = discounts.Select(d => MapToDiscountInfo(d)).ToList();

                return new DiscountListViewModel
                {
                    Discounts = discountInfos,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalCount,
                    SearchTerm = searchTerm,
                    IsActive = isActive,
                    TypeFilter = type
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discounts");
                return new DiscountListViewModel
                {
                    Discounts = new List<DiscountInfo>(),
                    CurrentPage = page,
                    TotalPages = 0,
                    TotalItems = 0,
                    SearchTerm = searchTerm,
                    IsActive = isActive,
                    TypeFilter = type
                };
            }
        }

        public async Task<Discount?> GetDiscountByIdAsync(int id)
        {
            try
            {
                return await _discountRepository.GetDiscountByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount by ID: {DiscountId}", id);
                return null;
            }
        }

        public async Task<Discount?> GetDiscountByCodeAsync(string code)
        {
            try
            {
                return await _discountRepository.GetDiscountByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount by code: {Code}", code);
                return null;
            }
        }

        public async Task<DiscountInfo> CreateDiscountAsync(CreateDiscountViewModel model)
        {
            try
            {
                if (!await ValidateDiscountDataAsync(model))
                    throw new InvalidOperationException("Discount data validation failed");

                var discount = new Discount
                {
                    Name = model.Name,
                    Description = model.Description,
                    Code = model.Code.ToUpper(),
                    Type = model.Type,
                    PercentageValue = model.PercentageValue,
                    FixedValue = model.FixedValue,
                    MinimumOrderValue = model.MinimumOrderValue,
                    MaximumDiscountAmount = model.MaximumDiscountAmount,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    MaxUsageCount = model.MaxUsageCount,
                    MaxUsagePerCustomer = model.MaxUsagePerCustomer,
                    Scope = model.Scope,
                    CreatedBy = "System" // You can pass this from the current user context
                };

                var createdDiscount = await _discountRepository.CreateDiscountAsync(discount);

                // Add products if scope is specific products
                if (model.Scope == DiscountScope.SpecificProducts && model.SelectedProductIds.Any())
                {
                    await AddProductsToDiscountAsync(createdDiscount.Id, model.SelectedProductIds);
                }

                // Add categories if scope is specific categories
                if (model.Scope == DiscountScope.SpecificCategories && model.SelectedCategoryIds.Any())
                {
                    await AddCategoriesToDiscountAsync(createdDiscount.Id, model.SelectedCategoryIds);
                }

                return MapToDiscountInfo(createdDiscount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating discount");
                throw new InvalidOperationException("Unable to create discount", ex);
            }
        }

        public async Task<bool> UpdateDiscountAsync(int id, EditDiscountViewModel model)
        {
            try
            {
                if (!await ValidateDiscountUpdateAsync(id, model))
                    return false;

                var existingDiscount = await _discountRepository.GetDiscountByIdAsync(id);
                if (existingDiscount == null)
                    return false;

                // Update properties
                existingDiscount.Name = model.Name;
                existingDiscount.Description = model.Description;
                existingDiscount.Code = model.Code.ToUpper();
                existingDiscount.Type = model.Type;
                existingDiscount.PercentageValue = model.PercentageValue;
                existingDiscount.FixedValue = model.FixedValue;
                existingDiscount.MinimumOrderValue = model.MinimumOrderValue;
                existingDiscount.MaximumDiscountAmount = model.MaximumDiscountAmount;
                existingDiscount.StartDate = model.StartDate;
                existingDiscount.EndDate = model.EndDate;
                existingDiscount.IsActive = model.IsActive;
                existingDiscount.IsFeatured = model.IsFeatured;
                existingDiscount.MaxUsageCount = model.MaxUsageCount;
                existingDiscount.MaxUsagePerCustomer = model.MaxUsagePerCustomer;
                existingDiscount.Scope = model.Scope;

                await _discountRepository.UpdateDiscountAsync(existingDiscount);

                // Handle product/category associations based on scope
                await UpdateDiscountAssociationsAsync(id, model);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating discount with ID: {DiscountId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteDiscountAsync(int id)
        {
            try
            {
                if (!await CanDeleteDiscountAsync(id))
                    return false;

                return await _discountRepository.DeleteDiscountAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting discount with ID: {DiscountId}", id);
                return false;
            }
        }

        public async Task<bool> DiscountExistsAsync(int id)
        {
            try
            {
                return await _discountRepository.DiscountExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if discount exists: {DiscountId}", id);
                return false;
            }
        }

        // ===== Featured & Active Discounts =====
        public async Task<IEnumerable<FeaturedDiscountViewModel>> GetFeaturedDiscountsAsync(int count = 5)
        {
            try
            {
                var discounts = await _discountRepository.GetFeaturedDiscountsAsync(count);
                return discounts.Select(d => new FeaturedDiscountViewModel
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    Code = d.Code,
                    Type = d.Type,
                    PercentageValue = d.PercentageValue,
                    FixedValue = d.FixedValue,
                    EndDate = d.EndDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting featured discounts");
                return new List<FeaturedDiscountViewModel>();
            }
        }

        public async Task<IEnumerable<DiscountInfo>> GetActiveDiscountsAsync()
        {
            try
            {
                var discounts = await _discountRepository.GetActiveDiscountsAsync();
                return discounts.Select(MapToDiscountInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting active discounts");
                return new List<DiscountInfo>();
            }
        }

        public async Task<IEnumerable<DiscountInfo>> GetExpiredDiscountsAsync()
        {
            try
            {
                var discounts = await _discountRepository.GetExpiredDiscountsAsync();
                return discounts.Select(MapToDiscountInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting expired discounts");
                return new List<DiscountInfo>();
            }
        }

        public async Task<IEnumerable<DiscountInfo>> GetUpcomingDiscountsAsync()
        {
            try
            {
                var discounts = await _discountRepository.GetUpcomingDiscountsAsync();
                return discounts.Select(MapToDiscountInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting upcoming discounts");
                return new List<DiscountInfo>();
            }
        }

        // ===== Product Discount Operations =====
        public async Task<ProductDiscountInfo?> GetProductDiscountInfoAsync(int productId)
        {
            try
            {
                var product = await _productRepository.GetProductByIdAsync(productId);
                if (product == null) return null;

                var activeDiscount = await _discountRepository.GetBestDiscountForProductAsync(productId);
                var minPrice = product.Variants?.Any() == true ? product.Variants.Min(v => v.Price) : 0;

                var discountedPrice = activeDiscount != null 
                    ? product.CalculateDiscountedPrice(minPrice, activeDiscount)
                    : minPrice;

                return new ProductDiscountInfo
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    OriginalPrice = minPrice,
                    DiscountedPrice = discountedPrice,
                    DiscountAmount = minPrice - discountedPrice,
                    DiscountPercentage = minPrice > 0 ? ((minPrice - discountedPrice) / minPrice) * 100 : 0,
                    ActiveDiscount = activeDiscount != null ? MapToDiscountInfo(activeDiscount) : null,
                    HasActiveDiscount = activeDiscount != null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting product discount info for product: {ProductId}", productId);
                return null;
            }
        }

        public async Task<IEnumerable<ProductInfo>> GetDiscountedProductsAsync(int page, int pageSize)
        {
            try
            {
                var products = await _discountRepository.GetProductsWithDiscountsAsync(page, pageSize);
                var productInfos = new List<ProductInfo>();

                foreach (var product in products)
                {
                    var activeDiscount = await _discountRepository.GetBestDiscountForProductAsync(product.Id);
                    var minPrice = product.Variants?.Any() == true ? product.Variants.Min(v => v.Price) : 0;
                    var maxPrice = product.Variants?.Any() == true ? product.Variants.Max(v => v.Price) : 0;

                    productInfos.Add(new ProductInfo
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        CategoryName = product.Category?.Name ?? "N/A",
                        BrandName = product.Brand?.Name ?? "N/A",
                        MinPrice = minPrice,
                        MaxPrice = maxPrice,
                        HasActiveDiscount = activeDiscount != null,
                        DiscountName = activeDiscount?.Name,
                        DiscountPercentage = activeDiscount?.PercentageValue,
                        DiscountAmount = activeDiscount?.FixedValue,
                        DiscountedMinPrice = activeDiscount != null 
                            ? product.CalculateDiscountedPrice(minPrice, activeDiscount)
                            : minPrice,
                        DiscountedMaxPrice = activeDiscount != null 
                            ? product.CalculateDiscountedPrice(maxPrice, activeDiscount)
                            : maxPrice,
                        VariantCount = product.Variants?.Count ?? 0
                    });
                }

                return productInfos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discounted products");
                return new List<ProductInfo>();
            }
        }

        public async Task<decimal> CalculateDiscountedPrice(int productId, decimal originalPrice)
        {
            try
            {
                var activeDiscount = await _discountRepository.GetBestDiscountForProductAsync(productId);
                if (activeDiscount == null) return originalPrice;

                var product = await _productRepository.GetProductByIdAsync(productId);
                return product?.CalculateDiscountedPrice(originalPrice, activeDiscount) ?? originalPrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating discounted price for product: {ProductId}", productId);
                return originalPrice;
            }
        }

        public async Task<decimal> CalculateDiscountAmount(int productId, decimal originalPrice)
        {
            try
            {
                var discountedPrice = await CalculateDiscountedPrice(productId, originalPrice);
                return originalPrice - discountedPrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating discount amount for product: {ProductId}", productId);
                return 0;
            }
        }

        // ===== Discount Application & Usage =====
        public async Task<DiscountApplicationResult> ApplyDiscountAsync(string discountCode, string customerEmail, decimal orderValue)
        {
            try
            {
                var discount = await _discountRepository.GetDiscountByCodeAsync(discountCode);
                if (discount == null)
                {
                    return new DiscountApplicationResult
                    {
                        IsSuccessful = false,
                        Message = "Mã khuy?n mãi không t?n t?i."
                    };
                }

                var canUse = await _discountRepository.CanUseDiscountAsync(discount.Id, customerEmail, orderValue);
                if (!canUse)
                {
                    return new DiscountApplicationResult
                    {
                        IsSuccessful = false,
                        Message = "Mã khuy?n mãi không th? s? d?ng ho?c ?ã h?t h?n."
                    };
                }

                var discountAmount = discount.Type == DiscountType.Percentage
                    ? Math.Min(orderValue * (discount.PercentageValue ?? 0) / 100, discount.MaximumDiscountAmount ?? decimal.MaxValue)
                    : Math.Min(discount.FixedValue ?? 0, orderValue);

                var finalPrice = Math.Max(0, orderValue - discountAmount);

                return new DiscountApplicationResult
                {
                    IsSuccessful = true,
                    Message = "Áp d?ng mã khuy?n mãi thành công.",
                    Discount = discount,
                    DiscountAmount = discountAmount,
                    FinalPrice = finalPrice
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while applying discount code: {Code}", discountCode);
                return new DiscountApplicationResult
                {
                    IsSuccessful = false,
                    Message = "Có l?i x?y ra khi áp d?ng mã khuy?n mãi."
                };
            }
        }

        public async Task<bool> CanUseDiscountAsync(string discountCode, string customerEmail, decimal orderValue)
        {
            try
            {
                var discount = await _discountRepository.GetDiscountByCodeAsync(discountCode);
                if (discount == null) return false;

                return await _discountRepository.CanUseDiscountAsync(discount.Id, customerEmail, orderValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if discount can be used: {Code}", discountCode);
                return false;
            }
        }

        public async Task<DiscountUsageInfo> RecordDiscountUsageAsync(int discountId, string customerEmail, decimal discountAmount, int? orderId = null)
        {
            try
            {
                var usage = new DiscountUsage
                {
                    DiscountId = discountId,
                    CustomerEmail = customerEmail,
                    SessionId = Guid.NewGuid().ToString(), // Generate session ID for guest users
                    OrderId = orderId,
                    DiscountAmount = discountAmount
                };

                var createdUsage = await _discountRepository.CreateDiscountUsageAsync(usage);
                var discount = await _discountRepository.GetDiscountByIdAsync(discountId);

                return new DiscountUsageInfo
                {
                    Id = createdUsage.Id,
                    DiscountName = discount?.Name ?? "",
                    DiscountCode = discount?.Code ?? "",
                    CustomerEmail = customerEmail,
                    DiscountAmount = discountAmount,
                    UsedAt = createdUsage.UsedAt,
                    OrderId = orderId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while recording discount usage for discount: {DiscountId}", discountId);
                throw;
            }
        }

        // ===== Product & Category Management =====
        public async Task<bool> AddProductsToDiscountAsync(int discountId, List<int> productIds)
        {
            try
            {
                var results = new List<bool>();
                foreach (var productId in productIds)
                {
                    var result = await _discountRepository.AddProductToDiscountAsync(discountId, productId);
                    results.Add(result);
                }
                return results.Any(r => r); // Return true if at least one product was added
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding products to discount: {DiscountId}", discountId);
                return false;
            }
        }

        public async Task<bool> RemoveProductsFromDiscountAsync(int discountId, List<int> productIds)
        {
            try
            {
                var results = new List<bool>();
                foreach (var productId in productIds)
                {
                    var result = await _discountRepository.RemoveProductFromDiscountAsync(discountId, productId);
                    results.Add(result);
                }
                return results.Any(r => r); // Return true if at least one product was removed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing products from discount: {DiscountId}", discountId);
                return false;
            }
        }

        public async Task<bool> AddCategoriesToDiscountAsync(int discountId, List<int> categoryIds)
        {
            try
            {
                var results = new List<bool>();
                foreach (var categoryId in categoryIds)
                {
                    var result = await _discountRepository.AddCategoryToDiscountAsync(discountId, categoryId);
                    results.Add(result);
                }
                return results.Any(r => r); // Return true if at least one category was added
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding categories to discount: {DiscountId}", discountId);
                return false;
            }
        }

        public async Task<bool> RemoveCategoriesFromDiscountAsync(int discountId, List<int> categoryIds)
        {
            try
            {
                var results = new List<bool>();
                foreach (var categoryId in categoryIds)
                {
                    var result = await _discountRepository.RemoveCategoryFromDiscountAsync(discountId, categoryId);
                    results.Add(result);
                }
                return results.Any(r => r); // Return true if at least one category was removed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing categories from discount: {DiscountId}", discountId);
                return false;
            }
        }

        public async Task<IEnumerable<ProductInfo>> GetDiscountProductsAsync(int discountId)
        {
            try
            {
                var products = await _discountRepository.GetDiscountProductsAsync(discountId);
                return products.Select(p => new ProductInfo
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CategoryName = p.Category?.Name ?? "N/A",
                    BrandName = p.Brand?.Name ?? "N/A",
                    MinPrice = p.Variants?.Any() == true ? p.Variants.Min(v => v.Price) : 0,
                    MaxPrice = p.Variants?.Any() == true ? p.Variants.Max(v => v.Price) : 0,
                    VariantCount = p.Variants?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount products for discount: {DiscountId}", discountId);
                return new List<ProductInfo>();
            }
        }

        public async Task<IEnumerable<CategoryInfo>> GetDiscountCategoriesAsync(int discountId)
        {
            try
            {
                var categories = await _discountRepository.GetDiscountCategoriesAsync(discountId);
                return categories.Select(c => new CategoryInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductCount = c.Products?.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount categories for discount: {DiscountId}", discountId);
                return new List<CategoryInfo>();
            }
        }

        // ===== Validation =====
        public async Task<bool> ValidateDiscountDataAsync(CreateDiscountViewModel model)
        {
            try
            {
                // Check if discount code is unique
                if (await _discountRepository.DiscountCodeExistsAsync(model.Code))
                    return false;

                // Validate discount type specific fields
                if (model.Type == DiscountType.Percentage && (!model.PercentageValue.HasValue || model.PercentageValue <= 0 || model.PercentageValue > 100))
                    return false;

                if (model.Type == DiscountType.FixedAmount && (!model.FixedValue.HasValue || model.FixedValue <= 0))
                    return false;

                // Validate date range
                if (model.StartDate >= model.EndDate)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating discount data");
                return false;
            }
        }

        public async Task<bool> ValidateDiscountUpdateAsync(int id, EditDiscountViewModel model)
        {
            try
            {
                // Check if discount exists
                if (!await _discountRepository.DiscountExistsAsync(id))
                    return false;

                // Check if discount code is unique (excluding current discount)
                if (await _discountRepository.DiscountCodeExistsAsync(model.Code, id))
                    return false;

                // Validate discount type specific fields
                if (model.Type == DiscountType.Percentage && (!model.PercentageValue.HasValue || model.PercentageValue <= 0 || model.PercentageValue > 100))
                    return false;

                if (model.Type == DiscountType.FixedAmount && (!model.FixedValue.HasValue || model.FixedValue <= 0))
                    return false;

                // Validate date range
                if (model.StartDate >= model.EndDate)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating discount update data for ID: {DiscountId}", id);
                return false;
            }
        }

        public async Task<bool> CanDeleteDiscountAsync(int id)
        {
            try
            {
                // Check if discount exists
                if (!await _discountRepository.DiscountExistsAsync(id))
                    return false;

                // Check if discount has been used
                var usageCount = await _discountRepository.GetDiscountUsageCountAsync(id);
                if (usageCount > 0)
                    return false; // Don't allow deletion if discount has been used

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if discount can be deleted: {DiscountId}", id);
                return false;
            }
        }

        public async Task<bool> IsDiscountCodeUniqueAsync(string code, int? excludeId = null)
        {
            try
            {
                return !await _discountRepository.DiscountCodeExistsAsync(code, excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if discount code is unique: {Code}", code);
                return false;
            }
        }

        // ===== Statistics & Reports =====
        public async Task<DiscountStatisticsViewModel> GetDiscountStatisticsAsync(int discountId)
        {
            try
            {
                var discount = await _discountRepository.GetDiscountByIdAsync(discountId);
                var usage = await _discountRepository.GetDiscountUsageAsync(discountId);

                var stats = new DiscountStatisticsViewModel
                {
                    Discount = discount != null ? MapToDiscountInfo(discount) : new DiscountInfo(),
                    TotalUsageCount = usage.Count(),
                    UniqueCustomerCount = usage.Select(u => u.CustomerEmail).Distinct().Count(),
                    TotalDiscountAmount = usage.Sum(u => u.DiscountAmount),
                    AverageDiscountAmount = usage.Any() ? usage.Average(u => u.DiscountAmount) : 0,
                    FirstUsed = usage.Any() ? usage.Min(u => u.UsedAt) : null,
                    LastUsed = usage.Any() ? usage.Max(u => u.UsedAt) : null,
                    DailyUsage = usage
                        .GroupBy(u => u.UsedAt.Date)
                        .Select(g => new DailyUsageStats
                        {
                            Date = g.Key,
                            UsageCount = g.Count(),
                            TotalAmount = g.Sum(u => u.DiscountAmount)
                        })
                        .OrderBy(d => d.Date)
                        .ToList()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount statistics for discount: {DiscountId}", discountId);
                return new DiscountStatisticsViewModel();
            }
        }

        public async Task<IEnumerable<DiscountUsageInfo>> GetDiscountUsageHistoryAsync(int discountId)
        {
            try
            {
                var usage = await _discountRepository.GetDiscountUsageAsync(discountId);
                var discount = await _discountRepository.GetDiscountByIdAsync(discountId);

                return usage.Select(u => new DiscountUsageInfo
                {
                    Id = u.Id,
                    DiscountName = discount?.Name ?? "",
                    DiscountCode = discount?.Code ?? "",
                    CustomerEmail = u.CustomerEmail,
                    DiscountAmount = u.DiscountAmount,
                    UsedAt = u.UsedAt,
                    OrderId = u.OrderId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount usage history for discount: {DiscountId}", discountId);
                return new List<DiscountUsageInfo>();
            }
        }

        public async Task<DashboardDiscountSummary> GetDiscountSummaryAsync()
        {
            try
            {
                var activeDiscounts = await _discountRepository.GetActiveDiscountsAsync();
                var featuredDiscounts = await _discountRepository.GetFeaturedDiscountsAsync();
                var expiredDiscounts = await _discountRepository.GetExpiredDiscountsAsync();
                var upcomingDiscounts = await _discountRepository.GetUpcomingDiscountsAsync();

                // Get usage statistics
                var allDiscounts = await _discountRepository.GetAllDiscountsAsync();
                var totalUsages = 0;
                var totalAmount = 0m;

                foreach (var discount in allDiscounts)
                {
                    var usageCount = await _discountRepository.GetDiscountUsageCountAsync(discount.Id);
                    var usage = await _discountRepository.GetDiscountUsageAsync(discount.Id);
                    totalUsages += usageCount;
                    totalAmount += usage.Sum(u => u.DiscountAmount);
                }

                return new DashboardDiscountSummary
                {
                    TotalActiveDiscounts = activeDiscounts.Count(),
                    TotalFeaturedDiscounts = featuredDiscounts.Count(),
                    TotalExpiredDiscounts = expiredDiscounts.Count(),
                    TotalUpcomingDiscounts = upcomingDiscounts.Count(),
                    TotalDiscountUsages = totalUsages,
                    TotalDiscountAmount = totalAmount,
                    RecentDiscounts = allDiscounts.Take(5).Select(MapToDiscountInfo),
                    TopPerformingDiscounts = allDiscounts
                        .Where(d => d.CurrentUsageCount > 0)
                        .OrderByDescending(d => d.CurrentUsageCount)
                        .Take(5)
                        .Select(MapToDiscountInfo)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting discount summary");
                return new DashboardDiscountSummary();
            }
        }

        // ===== Private Helper Methods =====
        private static DiscountInfo MapToDiscountInfo(Discount discount)
        {
            return new DiscountInfo
            {
                Id = discount.Id,
                Name = discount.Name,
                Code = discount.Code,
                Type = discount.Type,
                PercentageValue = discount.PercentageValue,
                FixedValue = discount.FixedValue,
                StartDate = discount.StartDate,
                EndDate = discount.EndDate,
                IsActive = discount.IsActive,
                IsFeatured = discount.IsFeatured,
                CurrentUsageCount = discount.CurrentUsageCount,
                MaxUsageCount = discount.MaxUsageCount,
                Scope = discount.Scope
            };
        }

        private async Task UpdateDiscountAssociationsAsync(int discountId, EditDiscountViewModel model)
        {
            // Clear existing associations and add new ones based on scope
            switch (model.Scope)
            {
                case DiscountScope.SpecificProducts:
                    // Remove all existing product associations
                    var existingProducts = await _discountRepository.GetDiscountProductsAsync(discountId);
                    var existingProductIds = existingProducts.Select(p => p.Id).ToList();
                    await RemoveProductsFromDiscountAsync(discountId, existingProductIds);

                    // Add new product associations
                    if (model.SelectedProductIds.Any())
                        await AddProductsToDiscountAsync(discountId, model.SelectedProductIds);
                    break;

                case DiscountScope.SpecificCategories:
                    // Remove all existing category associations
                    var existingCategories = await _discountRepository.GetDiscountCategoriesAsync(discountId);
                    var existingCategoryIds = existingCategories.Select(c => c.Id).ToList();
                    await RemoveCategoriesFromDiscountAsync(discountId, existingCategoryIds);

                    // Add new category associations
                    if (model.SelectedCategoryIds.Any())
                        await AddCategoriesToDiscountAsync(discountId, model.SelectedCategoryIds);
                    break;

                case DiscountScope.AllProducts:
                    // Remove all existing associations since this applies to all products
                    var allExistingProducts = await _discountRepository.GetDiscountProductsAsync(discountId);
                    var allExistingProductIds = allExistingProducts.Select(p => p.Id).ToList();
                    await RemoveProductsFromDiscountAsync(discountId, allExistingProductIds);

                    var allExistingCategories = await _discountRepository.GetDiscountCategoriesAsync(discountId);
                    var allExistingCategoryIds = allExistingCategories.Select(c => c.Id).ToList();
                    await RemoveCategoriesFromDiscountAsync(discountId, allExistingCategoryIds);
                    break;
            }
        }
    }
}