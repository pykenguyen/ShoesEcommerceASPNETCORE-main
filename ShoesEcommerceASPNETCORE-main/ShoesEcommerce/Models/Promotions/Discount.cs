using ShoesEcommerce.Models.Products;
using System.ComponentModel.DataAnnotations;

namespace ShoesEcommerce.Models.Promotions
{
    public class Discount
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public DiscountType Type { get; set; }

        // For percentage discounts (e.g., 15%)
        public decimal? PercentageValue { get; set; }

        // For fixed amount discounts (e.g., 50,000 VND)
        public decimal? FixedValue { get; set; }

        // Minimum order value to apply discount
        public decimal? MinimumOrderValue { get; set; }

        // Maximum discount amount for percentage discounts
        public decimal? MaximumDiscountAmount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false; // For displaying on homepage

        // Usage limits
        public int? MaxUsageCount { get; set; } // Total usage limit
        public int CurrentUsageCount { get; set; } = 0;
        public int? MaxUsagePerCustomer { get; set; } // Per customer limit

        // Scope - what products this applies to
        public DiscountScope Scope { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<DiscountProduct> DiscountProducts { get; set; } = new List<DiscountProduct>();
        public ICollection<DiscountCategory> DiscountCategories { get; set; } = new List<DiscountCategory>();
        public ICollection<DiscountUsage> DiscountUsages { get; set; } = new List<DiscountUsage>();

        // Computed properties
        public bool IsExpired => DateTime.Now > EndDate;
        public bool IsNotStarted => DateTime.Now < StartDate;
        public bool IsCurrentlyActive => IsActive && !IsExpired && !IsNotStarted;
        public bool HasUsageLimit => MaxUsageCount.HasValue;
        public bool IsUsageLimitReached => HasUsageLimit && CurrentUsageCount >= MaxUsageCount.Value;
        public bool CanBeUsed => IsCurrentlyActive && !IsUsageLimitReached;
    }

    public enum DiscountType
    {
        Percentage, // Giảm theo phần trăm
        FixedAmount // Giảm số tiền cố định
    }

    public enum DiscountScope
    {
        AllProducts,     // Tất cả sản phẩm
        SpecificProducts, // Sản phẩm cụ thể
        SpecificCategories // Danh mục cụ thể
    }
}
