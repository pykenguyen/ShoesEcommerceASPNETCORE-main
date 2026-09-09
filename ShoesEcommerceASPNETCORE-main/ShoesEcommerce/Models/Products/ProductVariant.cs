using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.Stocks;
using ShoesEcommerce.Models.Promotions;

namespace ShoesEcommerce.Models.Products
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // ✅ RELATIONSHIPS: One-to-One with Stock, One-to-Many with others
        public Stock? CurrentStock { get; set; }  // Navigation property
        public ICollection<StockEntry> StockEntries { get; set; } = new List<StockEntry>();
        public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        // ✅ COMPUTED PROPERTIES: Safe with null checking
        public int AvailableQuantity => CurrentStock?.AvailableQuantity ?? 0;
        public int ReservedQuantity => CurrentStock?.ReservedQuantity ?? 0;
        public int TotalStockQuantity => CurrentStock?.TotalQuantity ?? 0;

        public bool IsInStock => AvailableQuantity > 0;
        public bool IsLowStock => AvailableQuantity > 0 && AvailableQuantity <= 10;
        public bool IsOutOfStock => AvailableQuantity <= 0;
        public bool HasPendingStock => ReservedQuantity > 0;

        // ✅ DISCOUNT PROPERTIES: Now properly implemented
        public Discount? GetActiveDiscount() => Product?.GetActiveDiscount();
        public bool HasActiveDiscount => GetActiveDiscount() != null;
        public decimal DiscountedPrice => Product?.CalculateDiscountedPrice(Price) ?? Price;
        public decimal DiscountAmount => Product?.CalculateDiscountAmount(Price) ?? 0;
        public decimal DiscountPercentage => Price > 0 && DiscountAmount > 0 ? (DiscountAmount / Price) * 100 : 0;
    }
}