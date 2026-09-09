using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Stocks
{
    public class StockEntry
    {
        public int Id { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        // ✅ QUANTITY RECEIVED
        public int QuantityReceived { get; set; }

        // ✅ COST TRACKING
        public decimal UnitCost { get; set; }
        public decimal TotalCost => QuantityReceived * UnitCost;

        // ✅ RECEIPT INFO
        public DateTime EntryDate { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // ✅ STATUS
        public bool IsProcessed { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
    }
}
