using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Repositories;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Service for checkout business logic
    /// </summary>
    public class CheckoutService : ICheckoutService
    {
        private readonly CheckoutRepository _repository;
        private readonly IDiscountService _discountService;
        private readonly ILogger<CheckoutService> _logger;

        public CheckoutService(
            CheckoutRepository repository,
            IDiscountService discountService,
            ILogger<CheckoutService> logger)
        {
            _repository = repository;
            _discountService = discountService;
            _logger = logger;
        }

        public async Task<Cart?> GetCartForCheckoutAsync(int customerId, string sessionId)
        {
            try
            {
                _logger.LogInformation("Getting cart for checkout: CustomerId={CustomerId}, SessionId={SessionId}", 
                    customerId, sessionId);

                var cart = await _repository.GetCartWithItemsAsync(customerId, sessionId);

                if (cart == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning("Empty cart for customerId={CustomerId}, sessionId={SessionId}", 
                        customerId, sessionId);
                    return null;
                }

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for checkout");
                throw;
            }
        }

        public async Task<List<ShippingAddress>> GetCustomerAddressesAsync(int customerId)
        {
            try
            {
                return await _repository.GetCustomerAddressesAsync(customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer addresses");
                throw;
            }
        }

        public async Task<ShippingAddress?> CreateShippingAddressAsync(
            int customerId, string fullName, string phoneNumber, 
            string address, string city, string district)
        {
            try
            {
                _logger.LogInformation("Creating shipping address for customer {CustomerId}", customerId);

                // Validate input
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber) ||
                    string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(city) || 
                    string.IsNullOrWhiteSpace(district))
                {
                    _logger.LogWarning("Invalid shipping address data for customer {CustomerId}", customerId);
                    return null;
                }

                var shippingAddress = new ShippingAddress
                {
                    CustomerId = customerId,
                    FullName = fullName.Trim(),
                    PhoneNumber = phoneNumber.Trim(),
                    Address = address.Trim(),
                    City = city.Trim(),
                    District = district.Trim()
                };

                return await _repository.CreateShippingAddressAsync(shippingAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping address");
                throw;
            }
        }

        public async Task<Order?> PlaceOrderAsync(
            int customerId, string sessionId, int shippingAddressId, 
            string paymentMethod, string? discountCode)
        {
            try
            {
                _logger.LogInformation("Placing order: CustomerId={CustomerId}, PaymentMethod={PaymentMethod}", 
                    customerId, paymentMethod);

                // Get cart
                var cart = await _repository.GetCartWithItemsAsync(customerId, sessionId);
                if (cart == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning("Empty cart during order placement");
                    return null;
                }

                // Validate shipping address
                var shippingAddress = await _repository.GetShippingAddressAsync(shippingAddressId);
                if (shippingAddress == null)
                {
                    _logger.LogWarning("Invalid shipping address {AddressId}", shippingAddressId);
                    return null;
                }

                // ✅ FIX: Only get non-deleted cart items
                var activeCartItems = cart.CartItems.Where(ci => !ci.IsDeleted).ToList();
                if (!activeCartItems.Any())
                {
                    _logger.LogWarning("No active cart items during order placement");
                    return null;
                }

                // Calculate totals
                decimal subtotal = activeCartItems.Sum(ci => ci.ProductVariant.Price * ci.Quantity);
                decimal discountAmount = 0;
                int? discountId = null;
                decimal totalAmount = subtotal;

                // Apply discount if provided
                if (!string.IsNullOrWhiteSpace(discountCode))
                {
                    var customerEmail = ""; // Get from context or pass as parameter
                    var discountResult = await _discountService.ApplyDiscountAsync(
                        discountCode.ToUpper(), customerEmail, subtotal);

                    if (discountResult.IsSuccessful)
                    {
                        discountAmount = discountResult.DiscountAmount;
                        discountId = discountResult.Discount?.Id;
                        totalAmount = discountResult.FinalPrice;
                        
                        _logger.LogInformation("Discount applied: Code={Code}, Amount={Amount}", 
                            discountCode, discountAmount);
                    }
                }

                // Create order
                var order = new Order
                {
                    CustomerId = customerId,
                    ShippingAddressId = shippingAddressId,
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    OrderDetails = activeCartItems.Select(ci => new OrderDetail
                    {
                        ProductVariantId = ci.ProductVarientId,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.ProductVariant.Price,
                        Status = "Pending"
                    }).ToList()
                };

                // Create payment record
                order.Payment = new Models.Orders.Payment
                {
                    Method = paymentMethod,
                    Status = "Pending"
                };

                // ✅ FIX: Create Invoice with DRAFT status - only finalize after payment success
                order.Invoice = new Invoice
                {
                    InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
                    Amount = totalAmount,
                    IssuedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Status = InvoiceStatus.Draft, // ✅ Draft until payment confirmed
                    Currency = "VND"
                };

                _logger.LogInformation("Creating order with Draft Invoice: {InvoiceNumber}", order.Invoice.InvoiceNumber);

                // Save order
                order = await _repository.CreateOrderAsync(order);

                // Update Invoice number with Order ID for better tracking
                order.Invoice.InvoiceNumber = $"INV-{order.Id}-{DateTime.UtcNow:yyyyMMdd}";
                await _repository.UpdateOrderAsync(order);

                _logger.LogInformation("Order {OrderId} created with Draft Invoice {InvoiceNumber}, Status={Status}", 
                    order.Id, order.Invoice.InvoiceNumber, order.Invoice.Status);

                // Record discount usage if applied
                if (discountId.HasValue && discountAmount > 0)
                {
                    try
                    {
                        var customerEmail = ""; // Get from context
                        await _discountService.RecordDiscountUsageAsync(
                            discountId.Value, customerEmail, discountAmount, order.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error recording discount usage");
                        // Don't fail order if discount recording fails
                    }
                }

                // ✅ FIX: Clear cart with soft delete and link to order
                await _repository.ClearCartAsync(cart, order.Id);

                _logger.LogInformation("Order {OrderId} created successfully with Draft Invoice {InvoiceNumber}, CartItems soft-deleted", 
                    order.Id, order.Invoice.InvoiceNumber);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                throw;
            }
        }

        public async Task<(decimal subtotal, decimal discountAmount, decimal totalAmount)> CalculateOrderTotalsAsync(
            Cart? cart, string? discountCode, string customerEmail)
        {
            try
            {
                // Handle null cart
                if (cart == null || !cart.CartItems.Any())
                {
                    _logger.LogWarning("Cannot calculate totals: cart is null or empty");
                    return (0, 0, 0);
                }

                decimal subtotal = cart.CartItems.Sum(ci => ci.ProductVariant.Price * ci.Quantity);
                decimal discountAmount = 0;
                decimal totalAmount = subtotal;

                if (!string.IsNullOrWhiteSpace(discountCode))
                {
                    var result = await _discountService.ApplyDiscountAsync(
                        discountCode.ToUpper(), customerEmail, subtotal);

                    if (result.IsSuccessful)
                    {
                        discountAmount = result.DiscountAmount;
                        totalAmount = result.FinalPrice;
                    }
                }

                return (subtotal, discountAmount, totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order totals");
                throw;
            }
        }

        public async Task<(bool isValid, string errorMessage)> ValidateCheckoutAsync(int customerId, string sessionId)
        {
            try
            {
                var cart = await _repository.GetCartWithItemsAsync(customerId, sessionId);

                if (cart == null || !cart.CartItems.Any())
                {
                    return (false, "Giỏ hàng của bạn đang trống.");
                }

                // Validate product variants exist
                foreach (var item in cart.CartItems)
                {
                    if (item.ProductVariant == null)
                    {
                        return (false, "Có s?n ph?m trong gi? hàng không t?n t?i.");
                    }
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating checkout");
                return (false, "Có l?i x?y ra khi ki?m tra gi? hàng.");
            }
        }
    }
}
