using ShoesEcommerce.Models.Interactions;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Promotions;
using ShoesEcommerce.Helpers;

namespace ShoesEcommerce.Models.Products
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// SEO-friendly URL slug for the product
        /// Auto-generated from Name if not set
        /// </summary>
        public string? Slug { get; set; }
        
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public int BrandId { get; set; }
        public Brand Brand { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<QA> QAs { get; set; } = new List<QA>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

        // ✅ CORRECT: Navigation properties only - NO QUERIES
        public ICollection<DiscountProduct> DiscountProducts { get; set; } = new List<DiscountProduct>();

        /// <summary>
        /// Gets the URL-friendly slug for this product
        /// Uses stored slug if available, otherwise generates from name
        /// </summary>
        public string GetSlug()
        {
            if (!string.IsNullOrEmpty(Slug))
                return Slug;
            
            return Name.ToSlugWithId(Id);
        }

        /// <summary>
        /// Generates and sets the slug from the product name
        /// </summary>
        public void GenerateSlug()
        {
            Slug = Name.ToSlug();
        }

        // ✅ NEW: Method to get the active discount for this product
        public Discount? GetActiveDiscount()
        {
            return DiscountProducts?
                .Where(dp => dp.Discount != null && dp.Discount.CanBeUsed)
                .Select(dp => dp.Discount)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();
        }

        // ✅ UPDATED: Overloaded method without discount parameter for convenience
        public decimal CalculateDiscountedPrice(decimal originalPrice)
        {
            var activeDiscount = GetActiveDiscount();
            return CalculateDiscountedPrice(originalPrice, activeDiscount);
        }

        // ✅ CORRECT: Pure business logic that operates on loaded data
        public decimal CalculateDiscountedPrice(decimal originalPrice, Discount? discount)
        {
            if (discount == null || !discount.IsCurrentlyActive)
                return originalPrice;

            decimal discountedPrice = discount.Type == DiscountType.Percentage
                ? originalPrice * (1 - (discount.PercentageValue ?? 0) / 100)
                : Math.Max(0, originalPrice - (discount.FixedValue ?? 0));

            // Apply maximum discount limit for percentage discounts
            if (discount.Type == DiscountType.Percentage && discount.MaximumDiscountAmount.HasValue)
            {
                var maxDiscount = discount.MaximumDiscountAmount.Value;
                var actualDiscount = originalPrice - discountedPrice;
                if (actualDiscount > maxDiscount)
                {
                    discountedPrice = originalPrice - maxDiscount;
                }
            }

            return Math.Max(0, discountedPrice);
        }

        public decimal CalculateDiscountAmount(decimal originalPrice, Discount? discount)
        {
            return originalPrice - CalculateDiscountedPrice(originalPrice, discount);
        }

        // ✅ NEW: Overloaded method without discount parameter for convenience
        public decimal CalculateDiscountAmount(decimal originalPrice)
        {
            var activeDiscount = GetActiveDiscount();
            return CalculateDiscountAmount(originalPrice, activeDiscount);
        }
    }
}