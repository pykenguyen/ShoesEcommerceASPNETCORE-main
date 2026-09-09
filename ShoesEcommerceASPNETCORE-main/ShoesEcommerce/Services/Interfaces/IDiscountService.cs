using ShoesEcommerce.Models.Promotions;
using ShoesEcommerce.ViewModels.Promotion;
using ShoesEcommerce.ViewModels.Product;

namespace ShoesEcommerce.Services.Interfaces
{
    public interface IDiscountService
    {
        // ===== Discount Management =====
        Task<DiscountListViewModel> GetDiscountsAsync(string? searchTerm, bool? isActive, DiscountType? type, int page, int pageSize);
        Task<Discount?> GetDiscountByIdAsync(int id);
        Task<Discount?> GetDiscountByCodeAsync(string code);
        Task<DiscountInfo> CreateDiscountAsync(CreateDiscountViewModel model);
        Task<bool> UpdateDiscountAsync(int id, EditDiscountViewModel model);
        Task<bool> DeleteDiscountAsync(int id);
        Task<bool> DiscountExistsAsync(int id);

        // ===== Featured & Active Discounts =====
        Task<IEnumerable<FeaturedDiscountViewModel>> GetFeaturedDiscountsAsync(int count = 5);
        Task<IEnumerable<DiscountInfo>> GetActiveDiscountsAsync();
        Task<IEnumerable<DiscountInfo>> GetExpiredDiscountsAsync();
        Task<IEnumerable<DiscountInfo>> GetUpcomingDiscountsAsync();

        // ===== Product Discount Operations =====
        Task<ProductDiscountInfo?> GetProductDiscountInfoAsync(int productId);
        Task<IEnumerable<ProductInfo>> GetDiscountedProductsAsync(int page, int pageSize);
        Task<decimal> CalculateDiscountedPrice(int productId, decimal originalPrice);
        Task<decimal> CalculateDiscountAmount(int productId, decimal originalPrice);

        // ===== Discount Application & Usage =====
        Task<DiscountApplicationResult> ApplyDiscountAsync(string discountCode, string customerEmail, decimal orderValue);
        Task<bool> CanUseDiscountAsync(string discountCode, string customerEmail, decimal orderValue);
        Task<DiscountUsageInfo> RecordDiscountUsageAsync(int discountId, string customerEmail, decimal discountAmount, int? orderId = null);

        // ===== Discount Product & Category Management =====
        Task<bool> AddProductsToDiscountAsync(int discountId, List<int> productIds);
        Task<bool> RemoveProductsFromDiscountAsync(int discountId, List<int> productIds);
        Task<bool> AddCategoriesToDiscountAsync(int discountId, List<int> categoryIds);
        Task<bool> RemoveCategoriesFromDiscountAsync(int discountId, List<int> categoryIds);
        Task<IEnumerable<ProductInfo>> GetDiscountProductsAsync(int discountId);
        Task<IEnumerable<CategoryInfo>> GetDiscountCategoriesAsync(int discountId);

        // ===== Validation =====
        Task<bool> ValidateDiscountDataAsync(CreateDiscountViewModel model);
        Task<bool> ValidateDiscountUpdateAsync(int id, EditDiscountViewModel model);
        Task<bool> CanDeleteDiscountAsync(int id);
        Task<bool> IsDiscountCodeUniqueAsync(string code, int? excludeId = null);

        // ===== Statistics & Reports =====
        Task<DiscountStatisticsViewModel> GetDiscountStatisticsAsync(int discountId);
        Task<IEnumerable<DiscountUsageInfo>> GetDiscountUsageHistoryAsync(int discountId);
        Task<DashboardDiscountSummary> GetDiscountSummaryAsync();
    }

    // ===== Supporting Classes =====
    public class ProductDiscountInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
        public DiscountInfo? ActiveDiscount { get; set; }
        public bool HasActiveDiscount { get; set; }
    }

    public class DiscountApplicationResult
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; } = string.Empty;
        public Discount? Discount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
    }

    public class DiscountUsageInfo
    {
        public int Id { get; set; }
        public string DiscountName { get; set; } = string.Empty;
        public string DiscountCode { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; }
        public int? OrderId { get; set; }
    }

    public class DiscountStatisticsViewModel
    {
        public DiscountInfo Discount { get; set; } = new();
        public int TotalUsageCount { get; set; }
        public int UniqueCustomerCount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal AverageDiscountAmount { get; set; }
        public DateTime? FirstUsed { get; set; }
        public DateTime? LastUsed { get; set; }
        public List<DailyUsageStats> DailyUsage { get; set; } = new();
    }

    public class DailyUsageStats
    {
        public DateTime Date { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DashboardDiscountSummary
    {
        public int TotalActiveDiscounts { get; set; }
        public int TotalFeaturedDiscounts { get; set; }
        public int TotalExpiredDiscounts { get; set; }
        public int TotalUpcomingDiscounts { get; set; }
        public int TotalDiscountUsages { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public IEnumerable<DiscountInfo> RecentDiscounts { get; set; } = new List<DiscountInfo>();
        public IEnumerable<DiscountInfo> TopPerformingDiscounts { get; set; } = new List<DiscountInfo>();
    }
}