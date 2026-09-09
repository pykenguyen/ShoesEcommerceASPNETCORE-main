using ShoesEcommerce.Models.Accounts;

namespace ShoesEcommerce.Models.Orders
{
    public class Order
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public int ShippingAddressId { get; set; }
        public ShippingAddress ShippingAddress { get; set; }

        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
        public Payment Payment { get; set; }
        public Invoice Invoice { get; set; }
        public string Status { get; set; } // e.g. Pending, Confirmed, Completed

    }
}
