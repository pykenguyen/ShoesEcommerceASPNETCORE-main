using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Models.Orders;


namespace ShoesEcommerce.Models.Carts
{
    public class CartItem
    {
        public int Id { get; set; } 

        public int CartId { get; set; }
        public Cart Cart { get; set; }

        public int ProductVarientId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        public int Quantity { get; set; }

        // ✅ NEW: Tracking fields for AI analytics and purchase history
        
        /// <summary>Soft delete flag - items are marked as deleted after purchase instead of being removed</summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>When this cart item was added to cart</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When this cart item was last modified (quantity change, etc.)</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>When this item was purchased (converted to order)</summary>
        public DateTime? PurchasedAt { get; set; }

        /// <summary>Link to the order when this cart item was purchased</summary>
        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        /// <summary>When this item was soft deleted (purchased or manually removed)</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>Unit price at time of adding to cart (for price tracking/comparison)</summary>
        public decimal? PriceAtAddTime { get; set; }

        /// <summary>Reason for deletion: Purchased, Removed, Expired, etc.</summary>
        public string? DeletionReason { get; set; }
    }
}
