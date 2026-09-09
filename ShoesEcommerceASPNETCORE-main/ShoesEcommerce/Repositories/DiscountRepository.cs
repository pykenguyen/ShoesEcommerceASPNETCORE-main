using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Promotions;
using ShoesEcommerce.Repositories.Interfaces;

namespace ShoesEcommerce.Repositories
{
    public class DiscountRepository : IDiscountRepository
    {
        private readonly AppDbContext _context;

        public DiscountRepository(AppDbContext context)
        {
            _context = context;
        }

        // ===== Discount CRUD =====
        public async Task<IEnumerable<Discount>> GetAllDiscountsAsync()
        {
            return await _context.Discounts
                .Include(d => d.DiscountProducts)
                    .ThenInclude(dp => dp.Product)
                .Include(d => d.DiscountCategories)
                    .ThenInclude(dc => dc.Category)
                .Include(d => d.DiscountUsages)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Discount?> GetDiscountByIdAsync(int id)
        {
            return await _context.Discounts
                .Include(d => d.DiscountProducts)
                    .ThenInclude(dp => dp.Product)
                .Include(d => d.DiscountCategories)
                    .ThenInclude(dc => dc.Category)
                .Include(d => d.DiscountUsages)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Discount?> GetDiscountByCodeAsync(string code)
        {
            return await _context.Discounts
                .Include(d => d.DiscountProducts)
                    .ThenInclude(dp => dp.Product)
                .Include(d => d.DiscountCategories)
                    .ThenInclude(dc => dc.Category)
                .FirstOrDefaultAsync(d => d.Code == code);
        }

        public async Task<Discount> CreateDiscountAsync(Discount discount)
        {
            discount.CreatedAt = DateTime.UtcNow; // ? FIX: UTC
            discount.UpdatedAt = DateTime.UtcNow; // ? FIX: UTC
            
            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
            return discount;
        }

        public async Task<Discount> UpdateDiscountAsync(Discount discount)
        {
            discount.UpdatedAt = DateTime.UtcNow; // ? FIX: UTC
            
            _context.Entry(discount).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return discount;
        }

        public async Task<bool> DeleteDiscountAsync(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
                return false;

            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DiscountExistsAsync(int id)
        {
            return await _context.Discounts.AnyAsync(d => d.Id == id);
        }

        public async Task<bool> DiscountCodeExistsAsync(string code, int? excludeId = null)
        {
            var query = _context.Discounts.Where(d => d.Code == code);
            
            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);
                
            return await query.AnyAsync();
        }

        // ===== Active & Featured Discounts =====
        public async Task<IEnumerable<Discount>> GetActiveDiscountsAsync()
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            return await _context.Discounts
                .Where(d => d.IsActive && 
                           d.StartDate <= now && 
                           d.EndDate >= now)
                .OrderByDescending(d => d.IsFeatured)
                .ThenByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Discount>> GetFeaturedDiscountsAsync(int count = 5)
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            return await _context.Discounts
                .Where(d => d.IsActive && 
                           d.IsFeatured &&
                           d.StartDate <= now && 
                           d.EndDate >= now)
                .OrderByDescending(d => d.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Discount>> GetExpiredDiscountsAsync()
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            return await _context.Discounts
                .Where(d => d.EndDate < now)
                .OrderByDescending(d => d.EndDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Discount>> GetUpcomingDiscountsAsync()
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            return await _context.Discounts
                .Where(d => d.IsActive && d.StartDate > now)
                .OrderBy(d => d.StartDate)
                .ToListAsync();
        }

        // ===== Product-Discount Relationships =====
        public async Task<IEnumerable<Discount>> GetDiscountsForProductAsync(int productId)
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            
            // Get direct product discounts
            var productDiscounts = await _context.DiscountProducts
                .Where(dp => dp.ProductId == productId && 
                            dp.Discount.IsActive &&
                            dp.Discount.StartDate <= now && 
                            dp.Discount.EndDate >= now)
                .Select(dp => dp.Discount)
                .ToListAsync();

            // Get category discounts for this product
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product != null)
            {
                var categoryDiscounts = await _context.DiscountCategories
                    .Where(dc => dc.CategoryId == product.CategoryId && 
                                dc.Discount.IsActive &&
                                dc.Discount.StartDate <= now && 
                                dc.Discount.EndDate >= now)
                    .Select(dc => dc.Discount)
                    .ToListAsync();

                productDiscounts.AddRange(categoryDiscounts);
            }

            // Get all products discounts (scope = AllProducts)
            var allProductsDiscounts = await _context.Discounts
                .Where(d => d.Scope == DiscountScope.AllProducts &&
                           d.IsActive &&
                           d.StartDate <= now && 
                           d.EndDate >= now)
                .ToListAsync();

            productDiscounts.AddRange(allProductsDiscounts);

            return productDiscounts
                .Distinct()
                .OrderByDescending(d => d.IsFeatured)
                .ThenByDescending(d => d.Type == DiscountType.Percentage ? d.PercentageValue : d.FixedValue);
        }

        public async Task<Discount?> GetBestDiscountForProductAsync(int productId)
        {
            var discounts = await GetDiscountsForProductAsync(productId);
            return discounts.FirstOrDefault();
        }

        public async Task<IEnumerable<Product>> GetProductsWithDiscountsAsync(int page, int pageSize)
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Include(p => p.DiscountProducts)
                    .ThenInclude(dp => dp.Discount)
                .Where(p => 
                    // Direct product discounts
                    p.DiscountProducts.Any(dp => dp.Discount.IsActive && 
                                                 dp.Discount.StartDate <= now && 
                                                 dp.Discount.EndDate >= now) ||
                    // Category discounts
                    _context.DiscountCategories.Any(dc => dc.CategoryId == p.CategoryId &&
                                                          dc.Discount.IsActive &&
                                                          dc.Discount.StartDate <= now && 
                                                          dc.Discount.EndDate >= now) ||
                    // All products discounts
                    _context.Discounts.Any(d => d.Scope == DiscountScope.AllProducts &&
                                               d.IsActive &&
                                               d.StartDate <= now && 
                                               d.EndDate >= now))
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetProductsWithDiscountsCountAsync()
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            
            return await _context.Products
                .Where(p => 
                    // Direct product discounts
                    p.DiscountProducts.Any(dp => dp.Discount.IsActive && 
                                                 dp.Discount.StartDate <= now && 
                                                 dp.Discount.EndDate >= now) ||
                    // Category discounts
                    _context.DiscountCategories.Any(dc => dc.CategoryId == p.CategoryId &&
                                                          dc.Discount.IsActive &&
                                                          dc.Discount.StartDate <= now && 
                                                          dc.Discount.EndDate >= now) ||
                    // All products discounts
                    _context.Discounts.Any(d => d.Scope == DiscountScope.AllProducts &&
                                               d.IsActive &&
                                               d.StartDate <= now && 
                                               d.EndDate >= now))
                .CountAsync();
        }

        // ===== Category-Discount Relationships =====
        public async Task<IEnumerable<Discount>> GetDiscountsForCategoryAsync(int categoryId)
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            
            return await _context.DiscountCategories
                .Where(dc => dc.CategoryId == categoryId && 
                            dc.Discount.IsActive &&
                            dc.Discount.StartDate <= now && 
                            dc.Discount.EndDate >= now)
                .Select(dc => dc.Discount)
                .OrderByDescending(d => d.IsFeatured)
                .ThenByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryDiscountsAsync(int categoryId)
        {
            var now = DateTime.UtcNow; // ? FIX: UTC
            
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .Where(p => p.CategoryId == categoryId &&
                           _context.DiscountCategories.Any(dc => dc.CategoryId == categoryId &&
                                                                 dc.Discount.IsActive &&
                                                                 dc.Discount.StartDate <= now && 
                                                                 dc.Discount.EndDate >= now))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        // ===== Discount Usage =====
        public async Task<IEnumerable<DiscountUsage>> GetDiscountUsageAsync(int discountId)
        {
            return await _context.DiscountUsages
                .Where(du => du.DiscountId == discountId)
                .OrderByDescending(du => du.UsedAt)
                .ToListAsync();
        }

        public async Task<int> GetDiscountUsageCountAsync(int discountId)
        {
            return await _context.DiscountUsages
                .CountAsync(du => du.DiscountId == discountId);
        }

        public async Task<int> GetDiscountUsageCountByCustomerAsync(int discountId, string customerEmail)
        {
            return await _context.DiscountUsages
                .CountAsync(du => du.DiscountId == discountId && du.CustomerEmail == customerEmail);
        }

        public async Task<DiscountUsage> CreateDiscountUsageAsync(DiscountUsage usage)
        {
            usage.UsedAt = DateTime.UtcNow; // ? FIX: UTC
            
            _context.DiscountUsages.Add(usage);
            await _context.SaveChangesAsync();
            
            // Update discount usage count
            var discount = await _context.Discounts.FindAsync(usage.DiscountId);
            if (discount != null)
            {
                discount.CurrentUsageCount++;
                await _context.SaveChangesAsync();
            }
            
            return usage;
        }

        // ===== Discount Products & Categories Management =====
        public async Task<bool> AddProductToDiscountAsync(int discountId, int productId)
        {
            var existing = await _context.DiscountProducts
                .AnyAsync(dp => dp.DiscountId == discountId && dp.ProductId == productId);
                
            if (existing)
                return false;

            var discountProduct = new DiscountProduct
            {
                DiscountId = discountId,
                ProductId = productId
            };

            _context.DiscountProducts.Add(discountProduct);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveProductFromDiscountAsync(int discountId, int productId)
        {
            var discountProduct = await _context.DiscountProducts
                .FirstOrDefaultAsync(dp => dp.DiscountId == discountId && dp.ProductId == productId);
                
            if (discountProduct == null)
                return false;

            _context.DiscountProducts.Remove(discountProduct);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddCategoryToDiscountAsync(int discountId, int categoryId)
        {
            var existing = await _context.DiscountCategories
                .AnyAsync(dc => dc.DiscountId == discountId && dc.CategoryId == categoryId);
                
            if (existing)
                return false;

            var discountCategory = new DiscountCategory
            {
                DiscountId = discountId,
                CategoryId = categoryId
            };

            _context.DiscountCategories.Add(discountCategory);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCategoryFromDiscountAsync(int discountId, int categoryId)
        {
            var discountCategory = await _context.DiscountCategories
                .FirstOrDefaultAsync(dc => dc.DiscountId == discountId && dc.CategoryId == categoryId);
                
            if (discountCategory == null)
                return false;

            _context.DiscountCategories.Remove(discountCategory);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Product>> GetDiscountProductsAsync(int discountId)
        {
            return await _context.DiscountProducts
                .Where(dp => dp.DiscountId == discountId)
                .Select(dp => dp.Product)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetDiscountCategoriesAsync(int discountId)
        {
            return await _context.DiscountCategories
                .Where(dc => dc.DiscountId == discountId)
                .Select(dc => dc.Category)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        // ===== Search & Filtering =====
        public async Task<IEnumerable<Discount>> SearchDiscountsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllDiscountsAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Discounts
                .Where(d =>
                    d.Name.ToLower().Contains(searchTerm) ||
                    d.Code.ToLower().Contains(searchTerm) ||
                    d.Description.ToLower().Contains(searchTerm))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Discount>> GetDiscountsByTypeAsync(DiscountType type)
        {
            return await _context.Discounts
                .Where(d => d.Type == type)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Discount>> GetDiscountsByScopeAsync(DiscountScope scope)
        {
            return await _context.Discounts
                .Where(d => d.Scope == scope)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Discount>> GetPaginatedDiscountsAsync(int page, int pageSize, string? searchTerm = null, bool? isActive = null, DiscountType? type = null)
        {
            var query = _context.Discounts.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(searchTerm) ||
                    d.Code.ToLower().Contains(searchTerm) ||
                    d.Description.ToLower().Contains(searchTerm));
            }

            if (isActive.HasValue)
            {
                var now = DateTime.UtcNow; // ? FIX: UTC
                if (isActive.Value)
                {
                    query = query.Where(d => d.IsActive && 
                                           d.StartDate <= now && 
                                           d.EndDate >= now);
                }
                else
                {
                    query = query.Where(d => !d.IsActive || 
                                           d.StartDate > now || 
                                           d.EndDate < now);
                }
            }

            if (type.HasValue)
            {
                query = query.Where(d => d.Type == type.Value);
            }

            return await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalDiscountCountAsync(string? searchTerm = null, bool? isActive = null, DiscountType? type = null)
        {
            var query = _context.Discounts.AsQueryable();

            // Apply same filters as GetPaginatedDiscountsAsync
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(searchTerm) ||
                    d.Code.ToLower().Contains(searchTerm) ||
                    d.Description.ToLower().Contains(searchTerm));
            }

            if (isActive.HasValue)
            {
                var now = DateTime.UtcNow; // ? FIX: UTC
                if (isActive.Value)
                {
                    query = query.Where(d => d.IsActive && 
                                           d.StartDate <= now && 
                                           d.EndDate >= now);
                }
                else
                {
                    query = query.Where(d => !d.IsActive || 
                                           d.StartDate > now || 
                                           d.EndDate < now);
                }
            }

            if (type.HasValue)
            {
                query = query.Where(d => d.Type == type.Value);
            }

            return await query.CountAsync();
        }

        // ===== Validation =====
        public async Task<bool> CanUseDiscountAsync(int discountId, string customerEmail, decimal orderValue)
        {
            var discount = await GetDiscountByIdAsync(discountId);
            if (discount == null || !discount.CanBeUsed)
                return false;

            // Check minimum order value
            if (discount.MinimumOrderValue.HasValue && orderValue < discount.MinimumOrderValue.Value)
                return false;

            // Check usage limits
            if (discount.HasUsageLimit && discount.IsUsageLimitReached)
                return false;

            // Check per-customer usage limit
            if (discount.MaxUsagePerCustomer.HasValue)
            {
                var customerUsageCount = await GetDiscountUsageCountByCustomerAsync(discountId, customerEmail);
                if (customerUsageCount >= discount.MaxUsagePerCustomer.Value)
                    return false;
            }

            return true;
        }

        public async Task<bool> IsDiscountValidAsync(int discountId)
        {
            var discount = await GetDiscountByIdAsync(discountId);
            return discount?.IsCurrentlyActive ?? false;
        }

        public async Task<bool> HasReachedUsageLimitAsync(int discountId)
        {
            var discount = await GetDiscountByIdAsync(discountId);
            return discount?.IsUsageLimitReached ?? false;
        }

        public async Task<bool> HasCustomerReachedUsageLimitAsync(int discountId, string customerEmail)
        {
            var discount = await GetDiscountByIdAsync(discountId);
            if (discount?.MaxUsagePerCustomer == null)
                return false;

            var customerUsageCount = await GetDiscountUsageCountByCustomerAsync(discountId, customerEmail);
            return customerUsageCount >= discount.MaxUsagePerCustomer.Value;
        }
    }
}