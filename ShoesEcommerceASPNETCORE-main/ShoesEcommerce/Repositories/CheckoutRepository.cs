using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Orders;
using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Repositories
{
    /// <summary>
    /// Repository for checkout-related database operations
    /// </summary>
    public class CheckoutRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CheckoutRepository> _logger;

        public CheckoutRepository(AppDbContext context, ILogger<CheckoutRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Cart?> GetCartWithItemsAsync(int customerId, string sessionId)
        {
            try
            {
                if (customerId != 0)
                {
                    // ? FIX: Filter only non-deleted cart items
                    return await _context.Carts
                        .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                        .FirstOrDefaultAsync(c => c.Customer != null && c.Customer.Id == customerId);
                }
                else
                {
                    // ? FIX: Filter only non-deleted cart items
                    return await _context.Carts
                        .Include(c => c.CartItems.Where(ci => !ci.IsDeleted))
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                        .FirstOrDefaultAsync(c => c.SessionId == sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for customerId={CustomerId}, sessionId={SessionId}", 
                    customerId, sessionId);
                return null;
            }
        }

        public async Task<List<ShippingAddress>> GetCustomerAddressesAsync(int customerId)
        {
            try
            {
                return await _context.ShippingAddresses
                    .Where(a => a.CustomerId == customerId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addresses for customer {CustomerId}", customerId);
                return new List<ShippingAddress>();
            }
        }

        public async Task<ShippingAddress> CreateShippingAddressAsync(ShippingAddress address)
        {
            try
            {
                _context.ShippingAddresses.Add(address);
                await _context.SaveChangesAsync();
                return address;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping address for customer {CustomerId}", address.CustomerId);
                throw;
            }
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            try
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", order.Id);
                throw;
            }
        }

        public async Task ClearCartAsync(Cart cart, int? orderId = null)
        {
            try
            {
                // ? SOFT DELETE instead of hard delete for AI analytics
                // This preserves purchase history for: recommendation systems, behavior analysis, purchase patterns
                var now = DateTime.UtcNow;
                var activeItems = cart.CartItems.Where(ci => !ci.IsDeleted).ToList();
                
                foreach (var item in activeItems)
                {
                    item.IsDeleted = true;
                    item.DeletedAt = now;
                    item.UpdatedAt = now;
                    item.PurchasedAt = orderId.HasValue ? now : null;
                    item.OrderId = orderId;
                    item.DeletionReason = orderId.HasValue ? "Purchased" : "CheckoutCleared";
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cart soft-deleted successfully: CartId={CartId}, Items={ItemCount}, OrderId={OrderId}", 
                    cart.Id, activeItems.Count, orderId?.ToString() ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                throw;
            }
        }

        public async Task<ShippingAddress?> GetShippingAddressAsync(int addressId)
        {
            try
            {
                return await _context.ShippingAddresses.FindAsync(addressId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping address {AddressId}", addressId);
                return null;
            }
        }
    }
}
