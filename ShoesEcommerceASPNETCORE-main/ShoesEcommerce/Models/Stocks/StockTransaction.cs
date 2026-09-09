using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Stocks
{
    /// <summary>
    /// AUDIT TRAIL: All stock movements (IN/OUT/ADJUST)
    /// MULTIPLE RECORDS PER PRODUCT VARIANT (complete history)
    /// </summary>
    public class StockTransaction
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        // ✅ TRANSACTION TYPE
        public StockTransactionType Type { get; set; }

        // ✅ QUANTITY CHANGE (positive for IN, negative for OUT)
        public int QuantityChange { get; set; }

        // ✅ RESULTING BALANCES
        public int AvailableQuantityBefore { get; set; }
        public int AvailableQuantityAfter { get; set; }
        public int ReservedQuantityBefore { get; set; }
        public int ReservedQuantityAfter { get; set; }

        // ✅ TRANSACTION INFO
        public DateTime TransactionDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;

        // ✅ REFERENCE INFO
        public string ReferenceType { get; set; } = string.Empty; // "StockEntry", "Order", "Adjustment"
        public int? ReferenceId { get; set; }
    }

    public enum StockTransactionType
    {
        StockIn,        // From supplier
        StockOut,       // Sale/shipment
        Reserve,        // Reserve for order
        Release,        // Release reservation
        Adjustment,     // Manual adjustment
        Damage,         // Damaged goods
        Return,         // Customer return
        Transfer        // Location transfer
    }
}
