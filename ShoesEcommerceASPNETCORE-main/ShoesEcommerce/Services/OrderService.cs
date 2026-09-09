using Microsoft.EntityFrameworkCore;
using ShoesEcommerce.Data;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Models.ViewModels;
using ShoesEcommerce.Models.Carts;
using ShoesEcommerce.Models.Products;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(AppDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OrderViewModel> GetOrderByIdAsync(int orderId, int customerId)
        {
            try
            {
                _logger.LogInformation("Getting order {OrderId} for customer {CustomerId}", orderId, customerId);
                
                Order order;
                if (customerId == 0)
                {
                    // Admin: ignore customer filter
                    order = await _context.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.ShippingAddress)
                        .Include(o => o.Payment)
                        .Include(o => o.Invoice) // ✅ NEW: Include Invoice
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                        .FirstOrDefaultAsync(o => o.Id == orderId);
                }
                else
                {
                    // Customer: filter by customerId
                    order = await _context.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.ShippingAddress)
                        .Include(o => o.Payment)
                        .Include(o => o.Invoice) // ✅ NEW: Include Invoice
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customerId);
                }

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for customer {CustomerId}", orderId, customerId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved order {OrderId} for customer {CustomerId}", orderId, customerId);

                return new OrderViewModel
                {
                    Id = order.Id,
                    OrderNumber = $"ORD{order.Id:D6}",
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    PaymentStatus = order.Payment?.Status ?? "Chưa thanh toán",
                    OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        Id = od.Id,
                        ProductVariant = od.ProductVariant != null && od.ProductVariant.Product != null
                            ? new ProductVariantViewModel
                            {
                                Id = od.ProductVariant.Id,
                                Name = od.ProductVariant.Product.Name,
                                ImageUrl = od.ProductVariant.ImageUrl,
                                Color = od.ProductVariant.Color,
                                Size = od.ProductVariant.Size,
                                Price = od.UnitPrice
                            }
                            : new ProductVariantViewModel
                            {
                                Id = od.ProductVariant?.Id ?? 0,
                                Name = "(Sản phẩm đã bị xóa)",
                                ImageUrl = od.ProductVariant?.ImageUrl ?? "/images/no-image.svg",
                                Color = od.ProductVariant?.Color ?? "",
                                Size = od.ProductVariant?.Size ?? "",
                                Price = od.UnitPrice
                            },
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        SubTotal = od.Quantity * od.UnitPrice
                    }).ToList(),
                    ShippingAddress = new ShippingAddressViewModel
                    {
                        Id = order.ShippingAddress.Id,
                        FullName = order.ShippingAddress.FullName,
                        PhoneNumber = order.ShippingAddress.PhoneNumber,
                        Address = order.ShippingAddress.Address,
                        City = order.ShippingAddress.City,
                        District = order.ShippingAddress.District
                    },
                    Payment = new PaymentViewModel
                    {
                        Id = order.Payment?.Id ?? 0,
                        Method = order.Payment?.Method ?? "",
                        Status = order.Payment?.Status ?? "",
                        PaidAt = order.Payment?.PaidAt,
                        TransactionId = order.Payment?.TransactionId,
                        
                        // ✅ NEW: Invoice data
                        InvoiceNumber = order.Invoice?.InvoiceNumber,
                        InvoiceIssuedAt = order.Invoice?.IssuedAt,
                        
                        // ✅ NEW: VNPay specific fields
                        VnPayTxnRef = order.Invoice?.VnPayTxnRef,
                        VnPayBankCode = order.Invoice?.VnPayBankCode,
                        VnPayBankTranNo = order.Invoice?.VnPayBankTranNo,
                        VnPayCardType = order.Invoice?.VnPayCardType,
                        
                        // ✅ NEW: PayPal specific fields
                        PayPalOrderId = order.Invoice?.PayPalOrderId,
                        PayPalTransactionId = order.Invoice?.PayPalTransactionId,
                        AmountInUSD = order.Invoice?.AmountInUSD,
                        ExchangeRate = order.Invoice?.ExchangeRate
                    },
                    CustomerName = order.Customer != null ? order.Customer.FirstName + " " + order.Customer.LastName : "",
                    CustomerId = order.CustomerId,
                    CustomerEmail = order.Customer?.Email ?? "",
                    CustomerPhone = order.Customer?.PhoneNumber ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId} for customer {CustomerId}", orderId, customerId);
                return null;
            }
        }

        public async Task<List<OrderViewModel>> GetCustomerOrdersAsync(int customerId)
        {
            try
            {
                _logger.LogInformation("Getting orders for customer {CustomerId}", customerId);
                
                var orders = await _context.Orders
                    .Include(o => o.ShippingAddress)
                    .Include(o => o.Payment)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .Where(o => o.CustomerId == customerId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {OrderCount} orders for customer {CustomerId}", orders.Count, customerId);

                return orders.Select(order => new OrderViewModel
                {
                    Id = order.Id,
                    OrderNumber = $"ORD{order.Id:D6}",
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    PaymentStatus = order.Payment?.Status ?? "Chưa thanh toán",
                    OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        Id = od.Id,
                        ProductVariant = od.ProductVariant != null && od.ProductVariant.Product != null
                            ? new ProductVariantViewModel
                            {
                                Id = od.ProductVariant.Id,
                                Name = od.ProductVariant.Product.Name,
                                ImageUrl = od.ProductVariant.ImageUrl,
                                Color = od.ProductVariant.Color,
                                Size = od.ProductVariant.Size,
                                Price = od.UnitPrice
                            }
                            : new ProductVariantViewModel
                            {
                                Id = od.ProductVariant?.Id ?? 0,
                                Name = "(Sản phẩm đã bị xóa)",
                                ImageUrl = od.ProductVariant?.ImageUrl ?? "/images/no-image.svg",
                                Color = od.ProductVariant?.Color ?? "",
                                Size = od.ProductVariant?.Size ?? "",
                                Price = od.UnitPrice
                            },
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        SubTotal = od.Quantity * od.UnitPrice
                    }).ToList(),
                    ShippingAddress = new ShippingAddressViewModel
                    {
                        Id = order.ShippingAddress.Id,
                        FullName = order.ShippingAddress.FullName,
                        PhoneNumber = order.ShippingAddress.PhoneNumber,
                        Address = order.ShippingAddress.Address,
                        City = order.ShippingAddress.City,
                        District = order.ShippingAddress.District
                    },
                    Payment = new PaymentViewModel
                    {
                        Id = order.Payment?.Id ?? 0,
                        Method = order.Payment?.Method ?? "",
                        Status = order.Payment?.Status ?? "",
                        PaidAt = order.Payment?.PaidAt,
                        TransactionId = order.Payment?.TransactionId // ✅ ADD: Map TransactionId
                    }
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for customer {CustomerId}", customerId);
                return new List<OrderViewModel>();
            }
        }

        public async Task<Order> CreateOrderAsync(CreateOrderViewModel model, int customerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Creating order for customer {CustomerId} with shipping address {ShippingAddressId}", customerId, model.ShippingAddressId);

                // Lấy thông tin giỏ hàng thông qua Customer.CartId
                var customer = await _context.Customers
                    .Include(c => c.Cart)
                        .ThenInclude(cart => cart.CartItems.Where(ci => !ci.IsDeleted)) // Only get non-deleted items
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(c => c.Id == customerId);
                
                var cart = customer?.Cart;
                var activeCartItems = cart?.CartItems?.Where(ci => !ci.IsDeleted).ToList();

                if (activeCartItems == null || !activeCartItems.Any())
                {
                    _logger.LogWarning("Empty cart for customer {CustomerId} during order creation", customerId);
                    throw new InvalidOperationException("Giỏ hàng trống");
                }

                _logger.LogInformation("Cart found with {CartItemCount} items for customer {CustomerId}", activeCartItems.Count, customerId);

                // Tạo đơn hàng
                var totalAmount = activeCartItems.Sum(ci => ci.Quantity * ci.ProductVariant.Price);
                var order = new Order
                {
                    CustomerId = customerId,
                    ShippingAddressId = model.ShippingAddressId,
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    OrderDetails = new List<OrderDetail>()
                };

                // Tạo chi tiết đơn hàng
                foreach (var cartItem in activeCartItems)
                {
                    order.OrderDetails.Add(new OrderDetail
                    {
                        ProductVariantId = cartItem.ProductVarientId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.ProductVariant.Price,
                        Status = "Pending"
                    });
                }

                // Tạo thanh toán
                order.Payment = new Models.Orders.Payment
                {
                    Method = model.PaymentMethod,
                    Status = "Pending",
                    OrderId = order.Id
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // ✅ FIX: Create Invoice AFTER order is saved so orderId is available
                order.Invoice = new Invoice
                {
                    OrderId = order.Id,
                    InvoiceNumber = GenerateInvoiceNumber(order.Id),
                    IssuedAt = DateTime.UtcNow,
                    Amount = order.TotalAmount,
                    Status = InvoiceStatus.Draft,
                    Currency = "VND",
                    CreatedAt = DateTime.UtcNow
                };

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} created successfully for customer {CustomerId} with total amount {TotalAmount}, Invoice: {InvoiceNumber}", 
                    order.Id, customerId, totalAmount, order.Invoice.InvoiceNumber);

                // ✅ IMPROVED: Soft delete cart items instead of hard delete for AI analytics
                // This preserves purchase history for: recommendation systems, behavior analysis, purchase patterns
                var now = DateTime.UtcNow;
                foreach (var cartItem in activeCartItems)
                {
                    cartItem.IsDeleted = true;
                    cartItem.DeletedAt = now;
                    cartItem.PurchasedAt = now;
                    cartItem.OrderId = order.Id;
                    cartItem.DeletionReason = "Purchased";
                    cartItem.UpdatedAt = now;
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Cart items soft-deleted and linked to Order {OrderId} for customer {CustomerId}. {ItemCount} items preserved for analytics.", 
                    order.Id, customerId, activeCartItems.Count);

                return order;
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order for customer {CustomerId}", customerId);
                throw new InvalidOperationException("Có lỗi xảy ra trong quá trình tạo đơn hàng", ex);
            }
        }

        public async Task<CheckoutViewModel> GetCheckoutDataAsync(int customerId)
        {
            try
            {
                _logger.LogInformation("Getting checkout data for customer {CustomerId}", customerId);

                // Lấy thông tin giỏ hàng thông qua Customer.CartId
                var customer = await _context.Customers
                    .Include(c => c.Cart)
                        .ThenInclude(cart => cart.CartItems)
                            .ThenInclude(ci => ci.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(c => c.Id == customerId);
                
                var cart = customer?.Cart;
                var addresses = await GetCustomerShippingAddressesAsync(customerId);

                var cartItems = cart?.CartItems?.Select(ci => new CartItemViewModel
                {
                    Id = ci.Id,
                    ProductVariant = new ProductVariantViewModel
                    {
                        Id = ci.ProductVariant.Id,
                        Name = ci.ProductVariant.Product.Name,
                        ImageUrl = ci.ProductVariant.ImageUrl, // ✅ FIXED: Use ProductVariant.ImageUrl
                        Color = ci.ProductVariant.Color,
                        Size = ci.ProductVariant.Size,
                        Price = ci.ProductVariant.Price // ✅ FIXED: Use ProductVariant.Price
                    },
                    Quantity = ci.Quantity,
                    UnitPrice = ci.ProductVariant.Price, // ✅ FIXED: Use ProductVariant.Price
                    SubTotal = ci.Quantity * ci.ProductVariant.Price // ✅ FIXED: Use ProductVariant.Price
                }).ToList() ?? new List<CartItemViewModel>();

                var subTotal = cartItems.Sum(ci => ci.SubTotal);
                var shippingFee = 0m; // Có thể tính dựa trên địa chỉ
                var totalAmount = subTotal + shippingFee;

                _logger.LogInformation("Checkout data prepared for customer {CustomerId}: {CartItemsCount} items, subtotal: {SubTotal}, total: {TotalAmount}", 
                    customerId, cartItems.Count, subTotal, totalAmount);

                return new CheckoutViewModel
                {
                    CartItems = cartItems,
                    AvailableAddresses = addresses.Select(a => new ShippingAddressViewModel
                    {
                        Id = a.Id,
                        FullName = a.FullName,
                        PhoneNumber = a.PhoneNumber,
                        Address = a.Address,
                        City = a.City,
                        District = a.District
                    }).ToList(),
                    OrderInfo = new CreateOrderViewModel(),
                    SubTotal = subTotal,
                    ShippingFee = shippingFee,
                    TotalAmount = totalAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checkout data for customer {CustomerId}", customerId);
                return new CheckoutViewModel
                {
                    CartItems = new List<CartItemViewModel>(),
                    AvailableAddresses = new List<ShippingAddressViewModel>(),
                    OrderInfo = new CreateOrderViewModel(),
                    SubTotal = 0m,
                    ShippingFee = 0m,
                    TotalAmount = 0m
                };
            }
        }

        public async Task<ShippingAddress> CreateShippingAddressAsync(CreateShippingAddressViewModel model, int customerId)
        {
            try
            {
                _logger.LogInformation("Creating shipping address for customer {CustomerId} - {FullName}", customerId, model.FullName);

                var address = new ShippingAddress
                {
                    CustomerId = customerId,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    City = model.City,
                    District = model.District
                };

                _context.ShippingAddresses.Add(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Shipping address {AddressId} created successfully for customer {CustomerId}", address.Id, customerId);

                return address;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipping address for customer {CustomerId}", customerId);
                throw new InvalidOperationException("Có lỗi xảy ra khi tạo địa chỉ giao hàng", ex);
            }
        }

        public async Task<List<ShippingAddress>> GetCustomerShippingAddressesAsync(int customerId)
        {
            try
            {
                _logger.LogInformation("Getting shipping addresses for customer {CustomerId}", customerId);

                var addresses = await _context.ShippingAddresses
                    .Where(sa => sa.CustomerId == customerId)
                    .ToListAsync();

                _logger.LogInformation("Found {AddressCount} shipping addresses for customer {CustomerId}", addresses.Count, customerId);

                return addresses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping addresses for customer {CustomerId}", customerId);
                return new List<ShippingAddress>();
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                _logger.LogInformation("Updating order {OrderId} status to {Status}", orderId, status);

                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                    return false;
                }

                order.Status = status; // Cập nhật trạng thái đơn hàng

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} status updated successfully", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, string status)
        {
            try
            {
                _logger.LogInformation("Updating payment status for order {OrderId} to {Status}", orderId, status);

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);
                
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for order {OrderId}", orderId);
                    return false;
                }

                payment.Status = status;
                if (status == "Paid")
                    payment.PaidAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment status updated successfully for order {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            try
            {
                var lastOrder = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();

                var nextId = (lastOrder?.Id ?? 0) + 1;
                var orderNumber = $"ORD{nextId:D6}";

                _logger.LogInformation("Generated order number: {OrderNumber}", orderNumber);

                return orderNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating order number");
                // Fallback to timestamp-based number
                var fallbackNumber = $"ORD{DateTime.Now:yyyyMMddHHmmss}";
                _logger.LogWarning("Using fallback order number: {OrderNumber}", fallbackNumber);
                return fallbackNumber;
            }
        }

        /// <summary>
        /// Generate invoice number with format: INV-{orderId}-{yyyyMMdd}
        /// Example: INV-57-20250604
        /// </summary>
        public string GenerateInvoiceNumber(int orderId)
        {
            var invoiceNumber = $"INV-{orderId}-{DateTime.UtcNow:yyyyMMdd}";
            _logger.LogInformation("Generated invoice number: {InvoiceNumber} for order {OrderId}", invoiceNumber, orderId);
            return invoiceNumber;
        }

        public async Task<decimal> CalculateShippingFeeAsync(string city, string district)
        {
            try
            {
                _logger.LogInformation("Calculating shipping fee for {City}, {District}", city, district);

                // Logic tính phí vận chuyển dựa trên thành phố và quận/huyện
                // Có thể lấy từ database hoặc tính toán theo quy tắc
                decimal shippingFee = 30000m; // Mặc định 30,000 VND

                _logger.LogInformation("Calculated shipping fee: {ShippingFee} for {City}, {District}", shippingFee, city, district);

                return shippingFee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping fee for {City}, {District}", city, district);
                return 30000m; // Default fallback fee
            }
        }

        public async Task<List<OrderViewModel>> GetOrdersByStatusAsync(string status)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.ShippingAddress)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(o => o.Payment)
                .Where(o => o.Status == status && o.Payment != null)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => new OrderViewModel
            {
                Id = o.Id,
                OrderNumber = o.Id.ToString(),
                CreatedAt = o.CreatedAt,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.Payment?.Status ?? "",
                PaymentMethod = o.Payment?.Method ?? "",
                PaymentDate = o.Payment?.PaidAt,
                OrderDetails = o.OrderDetails.Select(od =>
                    od.ProductVariant != null && od.ProductVariant.Product != null
                    ? new OrderDetailViewModel
                    {
                        Id = od.Id,
                        ProductVariant = new ProductVariantViewModel
                        {
                            Id = od.ProductVariant.Id,
                            Name = od.ProductVariant.Product.Name,
                            ImageUrl = od.ProductVariant.ImageUrl,
                            Color = od.ProductVariant.Color,
                            Size = od.ProductVariant.Size,
                            Price = od.UnitPrice
                        },
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        SubTotal = od.Quantity * od.UnitPrice
                    }
                    : new OrderDetailViewModel
                    {
                        Id = od.Id,
                        ProductVariant = new ProductVariantViewModel
                        {
                            Id = od.ProductVariant?.Id ?? 0,
                            Name = "(Sản phẩm đã bị xóa)",
                            ImageUrl = od.ProductVariant?.ImageUrl ?? "/images/no-image.svg",
                            Color = od.ProductVariant?.Color ?? "",
                            Size = od.ProductVariant?.Size ?? "",
                            Price = od.UnitPrice
                        },
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        SubTotal = od.Quantity * od.UnitPrice
                    }
                ).ToList(),
                ShippingAddress = new ShippingAddressViewModel
                {
                    Id = o.ShippingAddress.Id,
                    FullName = o.ShippingAddress.FullName,
                    PhoneNumber = o.ShippingAddress.PhoneNumber,
                    Address = o.ShippingAddress.Address,
                    City = o.ShippingAddress.City,
                    District = o.ShippingAddress.District
                },
                Payment = o.Payment != null ? new PaymentViewModel
                {
                    Id = o.Payment.Id,
                    Method = o.Payment.Method,
                    Status = o.Payment.Status,
                    PaidAt = o.Payment.PaidAt,
                    TransactionId = o.Payment.TransactionId // ✅ ADD: Map TransactionId
                } : null,
                CustomerName = o.Customer != null ? o.Customer.FirstName + " " + o.Customer.LastName : "",
                CustomerId = o.CustomerId,
                CustomerEmail = o.Customer?.Email ?? "",
                CustomerPhone = o.Customer?.PhoneNumber ?? ""
            }).ToList();
        }

        public async Task<OrderViewModel?> GetOrderByOrderNumberAndPhoneAsync(string orderNumber, string phone)
        {
            try
            {
                _logger.LogInformation("Tracking order {OrderNumber} with phone {Phone}", orderNumber, phone);

                // Clean up order number - extract ID if format is ORD000001
                int orderId = 0;
                if (orderNumber.StartsWith("ORD", StringComparison.OrdinalIgnoreCase))
                {
                    var idPart = orderNumber.Substring(3);
                    int.TryParse(idPart, out orderId);
                }
                else
                {
                    int.TryParse(orderNumber, out orderId);
                }

                // Clean phone number
                var cleanPhone = phone.Replace(" ", "").Replace("-", "").Replace(".", "");
                if (cleanPhone.StartsWith("84"))
                {
                    cleanPhone = "0" + cleanPhone.Substring(2);
                }

                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.ShippingAddress)
                    .Include(o => o.Payment)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(o => 
                        o.Id == orderId && 
                        (o.ShippingAddress.PhoneNumber.Replace(" ", "").Replace("-", "") == cleanPhone ||
                         o.Customer.PhoneNumber.Replace(" ", "").Replace("-", "") == cleanPhone));

                if (order == null)
                {
                    _logger.LogWarning("Order not found for {OrderNumber} with phone {Phone}", orderNumber, phone);
                    return null;
                }

                _logger.LogInformation("Found order {OrderId} for tracking", order.Id);

                return new OrderViewModel
                {
                    Id = order.Id,
                    OrderNumber = $"ORD{order.Id:D6}",
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    PaymentStatus = order.Payment?.Status ?? "Chưa thanh toán",
                    OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        Id = od.Id,
                        ProductVariant = od.ProductVariant != null && od.ProductVariant.Product != null
                            ? new ProductVariantViewModel
                            {
                                Id = od.ProductVariant.Id,
                                Name = od.ProductVariant.Product.Name,
                                ImageUrl = od.ProductVariant.ImageUrl,
                                Color = od.ProductVariant.Color,
                                Size = od.ProductVariant.Size,
                                Price = od.UnitPrice
                            }
                            : new ProductVariantViewModel
                            {
                                Id = od.ProductVariant?.Id ?? 0,
                                Name = "(Sản phẩm đã bị xóa)",
                                ImageUrl = "/images/no-image.svg",
                                Color = "",
                                Size = "",
                                Price = od.UnitPrice
                            },
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        SubTotal = od.Quantity * od.UnitPrice
                    }).ToList(),
                    ShippingAddress = new ShippingAddressViewModel
                    {
                        Id = order.ShippingAddress.Id,
                        FullName = order.ShippingAddress.FullName,
                        PhoneNumber = order.ShippingAddress.PhoneNumber,
                        Address = order.ShippingAddress.Address,
                        City = order.ShippingAddress.City,
                        District = order.ShippingAddress.District
                    },
                    Payment = new PaymentViewModel
                    {
                        Id = order.Payment?.Id ?? 0,
                        Method = order.Payment?.Method ?? "",
                        Status = order.Payment?.Status ?? "",
                        PaidAt = order.Payment?.PaidAt,
                        TransactionId = order.Payment?.TransactionId
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order {OrderNumber}", orderNumber);
                return null;
            }
        }
    }
}
