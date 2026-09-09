using ShoesEcommerce.Models.Accounts;

namespace ShoesEcommerce.Models.Carts
{
    public class Cart
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Initialize as List instead of ICollection to ensure consistency
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        
        // Navigation property để truy cập Customer thông qua Customer.CartId
        public Customer? Customer { get; set; }
    }
}
