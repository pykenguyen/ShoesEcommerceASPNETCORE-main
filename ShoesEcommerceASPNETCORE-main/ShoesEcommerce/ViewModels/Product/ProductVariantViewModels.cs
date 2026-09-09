using ShoesEcommerce.ViewModels.Promotion;

namespace ShoesEcommerce.ViewModels.Product
{
    // ? NEW: Product Variant List View Model for displaying variants instead of products
    public class ProductVariantListViewModel
    {
        public IEnumerable<ProductVariantDisplayInfo> ProductVariants { get; set; } = new List<ProductVariantDisplayInfo>();
        public IEnumerable<FeaturedDiscountViewModel> FeaturedDiscounts { get; set; } = new List<FeaturedDiscountViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int PageSize { get; set; } = 12;
        public bool ShowDiscountsOnly { get; set; } = false;
    }

    // ? NEW: Product Variant Display Info for showing variants with product details
    public class ProductVariantDisplayInfo
    {
        public int Id { get; set; } // Variant ID
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsInStock => StockQuantity > 0;
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= 10;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Discount properties
        public bool HasActiveDiscount { get; set; }
        public string? DiscountName { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal DiscountedPrice { get; set; }
        
        // Additional display properties
        public string DisplayName => $"{ProductName} - {Color} / {Size}";
        public string FormattedPrice => HasActiveDiscount 
            ? $"{DiscountedPrice:C0} (t? {Price:C0})" 
            : Price.ToString("C0");
    }
}