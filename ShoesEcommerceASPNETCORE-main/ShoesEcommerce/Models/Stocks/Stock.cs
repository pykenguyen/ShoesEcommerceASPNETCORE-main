using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Stocks
{
    /// <summary>
    /// SINGLE SOURCE OF TRUTH: Current available stock for each product variant
    /// ONE RECORD PER PRODUCT VARIANT
    /// </summary>
    public class Stock
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        // ✅ CURRENT AVAILABLE QUANTITY
        public int AvailableQuantity { get; set; }
        
        // ✅ RESERVED QUANTITY (for pending orders)
        public int ReservedQuantity { get; set; }
        
        // ✅ AUDIT FIELDS
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; } = string.Empty;

        // ✅ COMPUTED PROPERTIES
        public int TotalQuantity => AvailableQuantity + ReservedQuantity;
        public bool IsInStock => AvailableQuantity > 0;
        public bool IsLowStock => AvailableQuantity > 0 && AvailableQuantity <= 10;
        public bool IsOutOfStock => AvailableQuantity <= 0;
    }
}
