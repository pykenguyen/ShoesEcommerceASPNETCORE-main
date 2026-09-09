using ShoesEcommerce.Models.Products;

namespace ShoesEcommerce.Models.Orders
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public string Status { get; set; } // e.g. Pending, Confirmed, Completed
    }
}
