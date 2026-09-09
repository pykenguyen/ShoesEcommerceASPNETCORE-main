using ShoesEcommerce.Models.Promotions;
using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Repositories.Interfaces
{
    public interface IDiscountRepository
    {
        // ===== Discount CRUD =====
        Task<IEnumerable<Discount>> GetAllDiscountsAsync();
        Task<Discount?> GetDiscountByIdAsync(int id);
        Task<Discount?> GetDiscountByCodeAsync(string code);
        Task<Discount> CreateDiscountAsync(Discount discount);
        Task<Discount> UpdateDiscountAsync(Discount discount);
        Task<bool> DeleteDiscountAsync(int id);
        Task<bool> DiscountExistsAsync(int id);
        Task<bool> DiscountCodeExistsAsync(string code, int? excludeId = null);

        // ===== Active & Featured Discounts =====
        Task<IEnumerable<Discount>> GetActiveDiscountsAsync();
        Task<IEnumerable<Discount>> GetFeaturedDiscountsAsync(int count = 5);
        Task<IEnumerable<Discount>> GetExpiredDiscountsAsync();
        Task<IEnumerable<Discount>> GetUpcomingDiscountsAsync();

        // ===== Product-Discount Relationships =====
        Task<IEnumerable<Discount>> GetDiscountsForProductAsync(int productId);
        Task<Discount?> GetBestDiscountForProductAsync(int productId);
        Task<IEnumerable<Product>> GetProductsWithDiscountsAsync(int page, int pageSize);
        Task<int> GetProductsWithDiscountsCountAsync();

        // ===== Category-Discount Relationships =====
        Task<IEnumerable<Discount>> GetDiscountsForCategoryAsync(int categoryId);
        Task<IEnumerable<Product>> GetProductsByCategoryDiscountsAsync(int categoryId);

        // ===== Discount Usage =====
        Task<IEnumerable<DiscountUsage>> GetDiscountUsageAsync(int discountId);
        Task<int> GetDiscountUsageCountAsync(int discountId);
        Task<int> GetDiscountUsageCountByCustomerAsync(int discountId, string customerEmail);
        Task<DiscountUsage> CreateDiscountUsageAsync(DiscountUsage usage);

        // ===== Discount Products & Categories Management =====
        Task<bool> AddProductToDiscountAsync(int discountId, int productId);
        Task<bool> RemoveProductFromDiscountAsync(int discountId, int productId);
        Task<bool> AddCategoryToDiscountAsync(int discountId, int categoryId);
        Task<bool> RemoveCategoryFromDiscountAsync(int discountId, int categoryId);
        Task<IEnumerable<Product>> GetDiscountProductsAsync(int discountId);
        Task<IEnumerable<Category>> GetDiscountCategoriesAsync(int discountId);

        // ===== Search & Filtering =====
        Task<IEnumerable<Discount>> SearchDiscountsAsync(string searchTerm);
        Task<IEnumerable<Discount>> GetDiscountsByTypeAsync(DiscountType type);
        Task<IEnumerable<Discount>> GetDiscountsByScopeAsync(DiscountScope scope);
        Task<IEnumerable<Discount>> GetPaginatedDiscountsAsync(int page, int pageSize, string? searchTerm = null, bool? isActive = null, DiscountType? type = null);
        Task<int> GetTotalDiscountCountAsync(string? searchTerm = null, bool? isActive = null, DiscountType? type = null);

        // ===== Validation =====
        Task<bool> CanUseDiscountAsync(int discountId, string customerEmail, decimal orderValue);
        Task<bool> IsDiscountValidAsync(int discountId);
        Task<bool> HasReachedUsageLimitAsync(int discountId);
        Task<bool> HasCustomerReachedUsageLimitAsync(int discountId, string customerEmail);
    }
}