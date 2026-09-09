using ShoesEcommerce.Models.Accounts;

namespace ShoesEcommerce.Models.Orders
{
    public class ShippingAddress
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}
