using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Orders;

namespace ShoesEcommerce.Services.Interfaces
{
    /// <summary>
    /// Service interface for checkout operations
    /// </summary>
    public interface ICheckoutService
    {
        /// <summary>
        /// Get cart with items for checkout
        /// </summary>
        Task<Cart?> GetCartForCheckoutAsync(int customerId, string sessionId);

        /// <summary>
        /// Get shipping addresses for a customer
        /// </summary>
        Task<List<ShippingAddress>> GetCustomerAddressesAsync(int customerId);

        /// <summary>
        /// Create a new shipping address
        /// </summary>
        Task<ShippingAddress?> CreateShippingAddressAsync(int customerId, string fullName, string phoneNumber, 
            string address, string city, string district);

        /// <summary>
        /// Place an order from cart
        /// </summary>
        Task<Order?> PlaceOrderAsync(int customerId, string sessionId, int shippingAddressId, 
            string paymentMethod, string? discountCode);

        /// <summary>
        /// Calculate order totals
        /// </summary>
        Task<(decimal subtotal, decimal discountAmount, decimal totalAmount)> CalculateOrderTotalsAsync(
            Cart? cart, string? discountCode, string customerEmail);

        /// <summary>
        /// Validate checkout prerequisites
        /// </summary>
        Task<(bool isValid, string errorMessage)> ValidateCheckoutAsync(int customerId, string sessionId);
    }
}
