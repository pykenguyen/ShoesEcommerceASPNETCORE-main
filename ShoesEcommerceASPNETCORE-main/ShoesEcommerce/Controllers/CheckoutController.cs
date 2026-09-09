using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services.Interfaces;
using System.Security.Claims;

namespace ShoesEcommerce.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IDiscountService _discountService;
        private readonly IOrderService _orderService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ICheckoutService checkoutService,
            IDiscountService discountService,
            IOrderService orderService,
            ILogger<CheckoutController> logger)
        {
            _checkoutService = checkoutService;
            _discountService = discountService;
            _orderService = orderService;
            _logger = logger;
        }

        private int GetCurrentCustomerId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return 0;

                return int.TryParse(userIdClaim, out int customerId) ? customerId : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current customer ID");
                return 0;
            }
        }

        private string GetCustomerEmail()
        {
            return User.Identity?.Name ?? "guest@temp.com";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Loading checkout page for customer {CustomerId} or session {SessionId}",
                    customerId, sessionId);

                var (isValid, errorMessage) = await _checkoutService.ValidateCheckoutAsync(customerId, sessionId);
                if (!isValid)
                {
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Index", "Cart");
                }

                var cart = await _checkoutService.GetCartForCheckoutAsync(customerId, sessionId);
                if (cart == null)
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                if (customerId != 0)
                {
                    var addresses = await _checkoutService.GetCustomerAddressesAsync(customerId);
                    ViewBag.Addresses = addresses;
                }
                else
                {
                    ViewBag.Addresses = new List<ShoesEcommerce.Models.Orders.ShippingAddress>();
                }

                var activeDiscounts = await _discountService.GetFeaturedDiscountsAsync(5);
                ViewBag.ActiveDiscounts = activeDiscounts;

                _logger.LogInformation("Checkout page loaded successfully for customer {CustomerId} with {CartItemCount} items",
                    customerId, cart.CartItems.Count);

                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["Error"] = "Có lỗi xảy ra khi tải trang thanh toán. Vui lòng thử lại.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string paymentMethod, string shippingAddress, string? discountCode)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Placing order for customer {CustomerId} with payment method {PaymentMethod}",
                    customerId, paymentMethod);

                if (string.IsNullOrWhiteSpace(paymentMethod))
                {
                    TempData["Error"] = "Vui lòng chọn phương thức thanh toán.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(shippingAddress) || !int.TryParse(shippingAddress, out int shippingAddressId))
                {
                    TempData["Error"] = "Địa chỉ giao hàng không hợp lệ.";
                    return RedirectToAction("Index");
                }

                var cart = await _checkoutService.GetCartForCheckoutAsync(customerId, sessionId);
                if (cart == null)
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                    return RedirectToAction("Index", "Cart");
                }

                var (subtotal, discountAmount, totalAmount) = await _checkoutService.CalculateOrderTotalsAsync(
                    cart, discountCode, GetCustomerEmail());

                _logger.LogInformation(
                    "Calculated order totals: Subtotal={Subtotal}, Discount={Discount}, Total={Total}",
                    subtotal, discountAmount, totalAmount);

                var order = await _checkoutService.PlaceOrderAsync(
                    customerId, sessionId, shippingAddressId, paymentMethod, discountCode);

                if (order == null)
                {
                    TempData["Error"] = "Không thể tạo đơn hàng. Vui lòng thử lại.";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("Order {OrderId} created successfully with total {TotalAmount}",
                    order.Id, totalAmount);

                // Redirect theo phương thức thanh toán
                if (paymentMethod == "PayPal")
                {
                    return RedirectToAction("PayPalCheckout", "Payment", new
                    {
                        orderId = order.Id,
                        subtotal = subtotal,
                        discountAmount = discountAmount,
                        totalAmount = totalAmount
                    });
                }
                else if (paymentMethod == "VNPay")
                {
                    // Redirect sang trang xác nhận VNPay trước khi tạo URL thanh toán
                    return RedirectToAction("VnPayCheckoutPage", "Payment", new { orderId = order.Id });
                }
                else if (paymentMethod == "COD")
                {
                    return RedirectToAction("SuccessCOD", new { orderId = order.Id });
                }

                TempData["Success"] = "Đặt hàng thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order for customer {CustomerId}", GetCurrentCustomerId());
                TempData["Error"] = "Có lỗi xảy ra trong quá trình đặt hàng. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrderAjax([FromForm] string paymentMethod, [FromForm] string shippingAddress, [FromForm] string? discountCode)
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var sessionId = HttpContext.Session.Id;

                _logger.LogInformation("Creating order via AJAX for customer {CustomerId} with payment method {PaymentMethod}",
                    customerId, paymentMethod);

                if (string.IsNullOrWhiteSpace(paymentMethod))
                {
                    return Json(new { success = false, error = "Vui lòng chọn phương thức thanh toán." });
                }

                if (string.IsNullOrWhiteSpace(shippingAddress) || !int.TryParse(shippingAddress, out int shippingAddressId))
                {
                    return Json(new { success = false, error = "Địa chỉ giao hàng không hợp lệ." });
                }

                var cart = await _checkoutService.GetCartForCheckoutAsync(customerId, sessionId);
                if (cart == null)
                {
                    return Json(new { success = false, error = "Giỏ hàng của bạn đang trống." });
                }

                var (subtotal, discountAmount, totalAmount) = await _checkoutService.CalculateOrderTotalsAsync(
                    cart, discountCode, GetCustomerEmail());

                // ✅ NEW: Log before calling PlaceOrderAsync
                _logger.LogInformation("About to place order: CustomerId={CustomerId}, PaymentMethod={PaymentMethod}, AddressId={AddressId}, Discount={DiscountCode}",
                    customerId, paymentMethod, shippingAddressId, discountCode ?? "None");

                var order = await _checkoutService.PlaceOrderAsync(
                    customerId, sessionId, shippingAddressId, paymentMethod, discountCode);

                if (order == null)
                {
                    _logger.LogError("PlaceOrderAsync returned null for customer {CustomerId}", customerId);
                    return Json(new { success = false, error = "Không thể tạo đơn hàng. Vui lòng thử lại." });
                }

                _logger.LogInformation("Order {OrderId} created via AJAX with total {TotalAmount}",
                    order.Id, totalAmount);

                return Json(new
                {
                    success = true,
                    orderId = order.Id,
                    subtotal = subtotal,
                    discountAmount = discountAmount,
                    totalAmount = totalAmount,
                    paymentMethod = paymentMethod
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR in CreateOrderAjax for customer {CustomerId}", GetCurrentCustomerId());
                
                // ✅ NEW: Log detailed error information
                _logger.LogError("Exception Type: {Type}", ex.GetType().FullName);
                _logger.LogError("Exception Message: {Message}", ex.Message);
                _logger.LogError("PaymentMethod: {PaymentMethod}", paymentMethod);
                _logger.LogError("ShippingAddress: {Address}", shippingAddress);
                _logger.LogError("DiscountCode: {DiscountCode}", discountCode ?? "None");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception Type: {InnerType}", ex.InnerException.GetType().FullName);
                    _logger.LogError("Inner Exception Message: {InnerMessage}", ex.InnerException.Message);
                }
                
                // ✅ IMPROVED: Return actual error message to help debugging
                return Json(new { 
                    success = false, 
                    error = $"Có lỗi xảy ra: {ex.Message}" // Include actual error
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShippingAddress(
            [FromForm] string fullName, [FromForm] string phoneNumber,
            [FromForm] string address, [FromForm] string city, [FromForm] string district)
        {
            try
            {
                var customerId = GetCurrentCustomerId();

                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thêm địa chỉ." });
                }

                var shippingAddress = await _checkoutService.CreateShippingAddressAsync(
                    customerId, fullName, phoneNumber, address, city, district);

                if (shippingAddress == null)
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });
                }

                _logger.LogInformation("Shipping address {AddressId} created for customer {CustomerId}",
                    shippingAddress.Id, customerId);

                return Json(new
                {
                    success = true,
                    message = "Thêm địa chỉ thành công!",
                    address = new
                    {
                        id = shippingAddress.Id,
                        fullName = shippingAddress.FullName,
                        phoneNumber = shippingAddress.PhoneNumber,
                        address = shippingAddress.Address,
                        city = shippingAddress.City,
                        district = shippingAddress.District
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding shipping address for customer {CustomerId}", GetCurrentCustomerId());
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm địa chỉ. Vui lòng thử lại." });
            }
        }

        public async Task<IActionResult> SuccessCOD(int orderId)
        {
            try
            {
                _logger.LogInformation("Loading COD success page for order {OrderId}", orderId);

                // ✅ FIXED: Validate orderId
                if (orderId <= 0)
                {
                    _logger.LogWarning("Invalid orderId for COD success page: {OrderId}", orderId);
                    TempData["Error"] = "Mã đơn hàng không hợp lệ.";
                    return RedirectToAction("Index", "Home");
                }

                var customerId = GetCurrentCustomerId();
                
                // ✅ FIXED: Get actual order data from OrderService
                var order = await _orderService.GetOrderByIdAsync(orderId, customerId);
                
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for COD success page", orderId);
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("COD success page loaded for order {OrderId}", orderId);
                
                // ✅ FIXED: Pass the OrderViewModel to the view
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading COD success page for order {OrderId}", orderId);
                TempData["Error"] = "Có lỗi xảy ra khi tải trang xác nhận đơn hàng.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
