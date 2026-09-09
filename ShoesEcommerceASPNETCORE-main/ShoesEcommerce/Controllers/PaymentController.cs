using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Models.Payments.PayPal;
using ShoesEcommerce.Models.ViewModels;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Services.Interfaces;
using System.Security.Claims;
using ShoesEcommerce.Services.Payment;
using System.Net;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ShoesEcommerce.ViewModels.Payment;

namespace ShoesEcommerce.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IVnPayService _vnPayService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IPaymentRepository paymentRepository,
            IVnPayService vnPayService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _paymentRepository = paymentRepository;
            _vnPayService = vnPayService;
            this._logger = logger;
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

        // =========================================================================
        // --- PAYPAL ACTIONS --- 
        // =========================================================================

        /// <summary>
        /// PayPal checkout page - Displays PayPal button
        /// </summary>
        [HttpGet]
        public IActionResult PayPalCheckout(int orderId, decimal subtotal, decimal discountAmount, decimal totalAmount)
        {
            try
            {
                _logger.LogInformation(
                    "Loading PayPal checkout for order {OrderId}: Subtotal={Subtotal}, Discount={Discount}, Total={Total}",
                    orderId, subtotal, discountAmount, totalAmount);

                // Store values in TempData for the view - Convert decimals to strings
                TempData["Subtotal"] = subtotal.ToString("F2");
                TempData["DiscountAmount"] = discountAmount.ToString("F2");
                TempData["TotalAmount"] = totalAmount.ToString("F2");

                return View(orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PayPal checkout for order {OrderId}", orderId);
                TempData["Error"] = "Có l?i x?y ra khi t?i trang thanh toán PayPal.";
                return RedirectToAction("Index", "Checkout");
            }
        }

        /// <summary>
        /// Create PayPal order - Called from client-side JavaScript
        /// </summary>
        [HttpPost("/payment/create-paypal-order")]
        public async Task<IActionResult> CreatePayPalOrder([FromBody] CreatePayPalOrderRequest? request)
        {
            try
            {
                // ? Validate request
                if (request == null)
                {
                    _logger.LogError("CreatePayPalOrder called with null request body");
                    return BadRequest(new { error = "Request body is null. Please provide order details." });
                }

                if (request.OrderId <= 0)
                {
                    _logger.LogError("CreatePayPalOrder called with invalid OrderId: {OrderId}", request.OrderId);
                    return BadRequest(new { error = "Invalid order ID" });
                }

                if (request.TotalAmount <= 0)
                {
                    _logger.LogError("CreatePayPalOrder called with invalid TotalAmount: {TotalAmount}", request.TotalAmount);
                    return BadRequest(new { error = "Invalid total amount" });
                }

                _logger.LogInformation(
                    "Creating PayPal order: OrderId={OrderId}, Subtotal={Subtotal}, Discount={Discount}, Total={Total}",
                    request.OrderId, request.Subtotal, request.DiscountAmount, request.TotalAmount);

                // ? Verify order exists using repository
                if (!await _paymentRepository.OrderExistsAsync(request.OrderId))
                {
                    _logger.LogWarning("Order {OrderId} not found", request.OrderId);
                    return BadRequest(new { error = "Đơn hàng không t?n t?i" });
                }

                // ? Optional: Verify customer ownership
                var customerId = GetCurrentCustomerId();
                if (customerId != 0)
                {
                    if (!await _paymentRepository.OrderBelongsToCustomerAsync(request.OrderId, customerId))
                    {
                        _logger.LogWarning(
                            "Unauthorized access to order {OrderId} by customer {CustomerId}",
                            request.OrderId, customerId);
                        return BadRequest(new { error = "B?n không có quy?n truy c?p đơn hàng này" });
                    }
                }

                // Build return and cancel URLs
                var returnUrl = Url.Action(
                    "PayPalSuccess",
                    "Payment",
                    new { orderId = request.OrderId },
                    protocol: Request.Scheme,
                    host: Request.Host.ToString());

                var cancelUrl = Url.Action(
                    "PayPalCancel",
                    "Payment",
                    new { orderId = request.OrderId },
                    protocol: Request.Scheme,
                    host: Request.Host.ToString());

                // ? Create PayPal order via service
                var response = await _paymentService.CreatePayPalOrderAsync(
                    request.OrderId,
                    request.Subtotal,
                    request.DiscountAmount,
                    request.TotalAmount,
                    returnUrl!,
                    cancelUrl!);

                _logger.LogInformation(
                    "PayPal order created: PayPalOrderId={PayPalOrderId}, OrderId={OrderId}",
                    response.id, request.OrderId);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order. Request: {@Request}", request);
                return BadRequest(new { error = $"L?i t?o PayPal order: {ex.Message}" });
            }
        }

        /// <summary>
        /// Capture PayPal order - Called from client-side after user approves
        /// </summary>
        [HttpPost("/payment/capture-paypal-order")]
        public async Task<IActionResult> CapturePayPalOrder([FromQuery] string orderId, [FromQuery] int orderIdLocal)
        {
            try
            {
                _logger.LogInformation(
                    "Capturing PayPal order: PayPalOrderId={PayPalOrderId}, LocalOrderId={LocalOrderId}",
                    orderId, orderIdLocal);

                // ✅ FIX: Capture payment on PayPal via service with proper error handling
                CaptureOrderResponse response;
                try
                {
                    response = await _paymentService.CapturePayPalOrderAsync(orderId);
                }
                catch (Exception captureEx)
                {
                    _logger.LogError(captureEx, "PayPal capture failed for order {PayPalOrderId}", orderId);
                    
                    // ✅ FIX: Rollback invoice and order status on capture failure
                    await _paymentService.HandlePaymentFailureAsync(orderIdLocal, $"PayPal capture failed: {captureEx.Message}");
                    
                    return BadRequest(new { error = "Không thể xác nhận thanh toán PayPal. Vui lòng thử lại." });
                }

                // Check capture status
                var capture = response.purchase_units?.FirstOrDefault()?.payments?.captures?.FirstOrDefault();

                if (capture == null)
                {
                    _logger.LogError("No capture information found for PayPal order {PayPalOrderId}", orderId);
                    
                    // ✅ FIX: Rollback on missing capture info
                    await _paymentService.HandlePaymentFailureAsync(orderIdLocal, "No capture information from PayPal");
                    
                    return BadRequest(new { error = "Không thể xác nhận thanh toán" });
                }

                var isSuccessful = capture.status == "COMPLETED";
                var paidAt = capture.create_time;
                var transactionId = capture.id;

                if (isSuccessful)
                {
                    // ✅ FIX: Complete payment properly (updates invoice to Paid status)
                    await _paymentService.CompletePaymentAsync(orderIdLocal, transactionId!, paidAt);
                    
                    _logger.LogInformation(
                        "PayPal payment captured successfully: PayPalOrderId={PayPalOrderId}, LocalOrderId={LocalOrderId}, TransactionId={TransactionId}",
                        orderId, orderIdLocal, transactionId);
                }
                else
                {
                    // ✅ FIX: Handle failed capture
                    await _paymentService.HandlePaymentFailureAsync(orderIdLocal, $"PayPal capture status: {capture.status}");
                    
                    _logger.LogWarning(
                        "PayPal capture not completed: PayPalOrderId={PayPalOrderId}, Status={Status}",
                        orderId, capture.status);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order {PayPalOrderId}", orderId);
                
                // ✅ FIX: Ensure rollback on any error
                try
                {
                    await _paymentService.HandlePaymentFailureAsync(orderIdLocal, $"Unexpected error: {ex.Message}");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Failed to rollback payment for order {OrderId}", orderIdLocal);
                }
                
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// PayPal success redirect page
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> PayPalSuccess(int orderId, string? token)
        {
            try
            {
                _logger.LogInformation(
                    "PayPal success redirect: OrderId={OrderId}, Token={Token}",
                    orderId, token);

                // ✅ IMPROVED: Better validation with logging
                if (orderId <= 0)
                {
                    _logger.LogWarning("PayPalSuccess called with invalid orderId: {OrderId}. This may be from a stale browser tab or bookmark.", orderId);
                    
                    // ✅ IMPROVED: Check if this is a direct navigation (no token = likely stale)
                    if (string.IsNullOrEmpty(token))
                    {
                        // Don't show error, just redirect silently
                        _logger.LogInformation("No token provided, likely stale navigation. Redirecting to Order page.");
                        return RedirectToAction("Index", "Order");
                    }
                    
                    TempData["Error"] = "Mã đơn hàng không hợp lệ. Vui lòng kiểm tra đơn hàng của bạn.";
                    return RedirectToAction("Index", "Order");
                }

                // Get order with all details
                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found on PayPal success", orderId);
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Order");
                }

                // ✅ NEW: Verify order has required data
                if (order.OrderDetails == null || !order.OrderDetails.Any())
                {
                    _logger.LogWarning("Order {OrderId} has no order details", orderId);
                    TempData["Error"] = "Đơn hàng không có sản phẩm.";
                    return RedirectToAction("Index", "Order");
                }

                // ✅ NEW: Build comprehensive ViewModel like VNPay
                var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
                var discountAmount = subtotal - order.TotalAmount;

                var viewModel = new PayPalSuccessViewModel
                {
                    OrderId = order.Id,
                    OrderCreatedAt = order.CreatedAt,
                    OrderStatus = order.Status,
                    TotalAmount = order.TotalAmount,
                    Subtotal = subtotal,
                    DiscountAmount = discountAmount > 0 ? discountAmount : 0,

                    TransactionId = order.Payment?.TransactionId ?? "",
                    PaymentMethod = "PayPal",
                    PaymentStatus = order.Payment?.Status ?? "Paid",
                    PaidAt = order.Payment?.PaidAt,

                    // Invoice info
                    InvoiceNumber = order.Invoice?.InvoiceNumber ?? $"INV-{order.Id}-{DateTime.UtcNow:yyyyMMdd}",
                    InvoiceIssuedAt = order.Invoice?.IssuedAt,
                    InvoiceStatus = order.Invoice?.Status ?? Models.Orders.InvoiceStatus.Paid,

                    // PayPal specific
                    PayPalOrderId = order.Invoice?.PayPalOrderId ?? "",
                    PayPalTransactionId = order.Invoice?.PayPalTransactionId ?? order.Payment?.TransactionId ?? "",
                    AmountInUSD = order.Invoice?.AmountInUSD,
                    ExchangeRate = order.Invoice?.ExchangeRate,
                    PayPalToken = token ?? "",

                    // Customer info
                    CustomerName = order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}" : "",
                    CustomerEmail = order.Customer?.Email ?? "",
                    CustomerPhone = order.Customer?.PhoneNumber ?? "",

                    // Shipping address
                    ShippingFullName = order.ShippingAddress?.FullName ?? "",
                    ShippingPhone = order.ShippingAddress?.PhoneNumber ?? "",
                    ShippingAddress = order.ShippingAddress?.Address ?? "",
                    ShippingDistrict = order.ShippingAddress?.District ?? "",
                    ShippingCity = order.ShippingAddress?.City ?? "",

                    // Order items
                    Items = order.OrderDetails.Select(od => new OrderItemViewModel
                    {
                        ProductName = od.ProductVariant?.Product?.Name ?? "Sản phẩm",
                        VariantColor = od.ProductVariant?.Color ?? "",
                        VariantSize = od.ProductVariant?.Size ?? "",
                        ImageUrl = od.ProductVariant?.ImageUrl ?? "/images/no-image.svg",
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice
                    }).ToList()
                };

                _logger.LogInformation(
                    "PayPalSuccess page loaded: OrderId={OrderId}, TransactionId={TxnId}, Invoice={Invoice}, CustomerId={CustomerId}, PaymentStatus={Status}",
                    orderId, viewModel.TransactionId, viewModel.InvoiceNumber, order.CustomerId, viewModel.PaymentStatus);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PayPal success page for order {OrderId}", orderId);
                
                // ✅ IMPROVED: Better UX - redirect to order list
                TempData["Info"] = "Đơn hàng của bạn đã được xử lý. Vui lòng kiểm tra danh sách đơn hàng.";
                return RedirectToAction("Index", "Order");
            }
        }

        /// <summary>
        /// PayPal cancel redirect page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PayPalCancel(int orderId)
        {
            try
            {
                _logger.LogInformation("PayPal payment cancelled for order {OrderId}", orderId);

                // ✅ FIX: Use new cancellation handler that properly cancels invoice
                await _paymentService.HandlePaymentCancellationAsync(orderId);

                // Get order via repository
                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found on PayPal cancel", orderId);
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.OrderId = orderId;
                ViewBag.OrderTotal = order.TotalAmount;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling PayPal cancellation for order {OrderId}", orderId);
                TempData["Warning"] = "Thanh toán đã bị hủy.";
                return RedirectToAction("Index", "Cart");
            }
        }

        /// <summary>
        /// Check payment status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int orderId)
        {
            try
            {
                // ? Get payment via service
                var payment = await _paymentService.GetPaymentByOrderIdAsync(orderId);

                if (payment == null)
                {
                    return NotFound(new { error = "Không t?m th?y thông tin thanh toán" });
                }

                return Ok(new
                {
                    orderId = payment.OrderId,
                    method = payment.Method,
                    status = payment.Status,
                    paidAt = payment.PaidAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking payment status for order {OrderId}", orderId);
                return BadRequest(new { error = ex.Message });
            }
        }

        // =========================================================================
        // --- VNPAY ACTIONS ---
        // ==========================================================================
        /// <summary>
        /// VNPay checkout page - hiển thị thông tin đơn hàng trước khi redirect sang VNPay
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VnPayCheckoutPage(int orderId)
        {
            var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);
            var customerId = GetCurrentCustomerId();

            if (order == null || (customerId != 0 && order.CustomerId != customerId))
            {
                TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền.";
                return RedirectToAction("Index", "Cart");
            }

            return View("VnPayCheckout", order);
        }

        /// <summary>
        /// VNPay checkout - Redirect to VNPay payment gate
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> VnPayCheckout(int orderId, decimal amount)
        {
            try
            {
                _logger.LogInformation("Initiating VNPay checkout for order {OrderId} with amount {Amount}", orderId, amount);

                var customerId = GetCurrentCustomerId();
                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null || (customerId != 0 && order.CustomerId != customerId))
                {
                    _logger.LogWarning("VNPay checkout blocked. Order {OrderId} not found or unauthorized for customer {CustomerId}", orderId, customerId);
                    TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền.";
                    return RedirectToAction("Index", "Cart");
                }

                if (order.OrderDetails == null || !order.OrderDetails.Any())
                {
                    _logger.LogWarning("Order {OrderId} has no details. Cannot proceed VNPay checkout.", orderId);
                    TempData["Error"] = "Đơn hàng không có sản phẩm.";
                    return RedirectToAction("Index", "Cart");
                }

                // Prepare invoice/payment records and use server-side amount
                var preparedOrder = await _paymentService.PrepareVnPayPaymentAsync(orderId);
                var actualAmount = preparedOrder.TotalAmount;

                if (actualAmount <= 0)
                {
                    _logger.LogError("Order {OrderId} has invalid total amount {Amount} for VNPay", orderId, actualAmount);
                    TempData["Error"] = "Số tiền thanh toán không hợp lệ.";
                    return RedirectToAction("Index", "Cart");
                }

                if (amount != actualAmount)
                {
                    _logger.LogWarning("VNPay amount tampering detected for order {OrderId}. Client {ClientAmount} vs Actual {ActualAmount}", orderId, amount, actualAmount);
                }

                var paymentUrl = _vnPayService.CreatePaymentUrl(orderId, actualAmount, HttpContext);
 
                 if (string.IsNullOrEmpty(paymentUrl))
                 {
                     _logger.LogError("Failed to create VNPay payment URL for order {OrderId}", orderId);
                     TempData["Error"] = "Lỗi tạo liên kết thanh toán VNPay.";
                     return RedirectToAction("Index", "Checkout");
                 }
 
                 return Redirect(paymentUrl);
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error during VNPay checkout for order {OrderId}", orderId);
                 TempData["Error"] = "Có lỗi xảy ra khi chuyển hướng thanh toán VNPay.";
                 return RedirectToAction("Index", "Checkout");
             }
         }

        /// <summary>
        /// Generate secure token from orderId and transactionId
        /// </summary>
        private static string GenerateSecureToken(int orderId, string? transactionId)
        {
            var data = $"{orderId}_{transactionId ?? ""}_{DateTime.UtcNow:yyyyMMdd}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "")[..16];
        }

        /// <summary>
        /// VNPay return handler - VNPay calls this after payment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                _logger.LogInformation("Receiving VNPay return callback: Query={Query}", Request.QueryString);

                var response = _vnPayService.ProcessReturn(Request.Query);

                if (!response.IsSuccess)
                {
                    _logger.LogWarning("VNPay hash validation failed. Response Code: {Code}", response.Vnp_ResponseCode);
                    TempData["Error"] = "Xác minh thanh toán không hợp lệ hoặc bị lỗi.";
                    return RedirectToAction("Index", "Home");
                }

                // ✅ UPDATED: Parse OrderId from new TxnRef format: INV-{OrderId}-{Date}-{UniqueCode}
                // Example: INV-63-20260108-A1B2 -> OrderId = 63
                var txnRef = response.Vnp_TxnRef ?? "";
                int localOrderId = 0;
                
                if (txnRef.StartsWith("INV-"))
                {
                    // New format: INV-{OrderId}-{Date}-{UniqueCode}
                    var parts = txnRef.Split('-');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedId))
                    {
                        localOrderId = parsedId;
                    }
                }
                else if (txnRef.Contains("_"))
                {
                    // Legacy format: {OrderId}_{tick}
                    var orderId = txnRef.Split('_').FirstOrDefault();
                    int.TryParse(orderId, out localOrderId);
                }
                
                if (localOrderId <= 0)
                {
                    _logger.LogError("Could not parse OrderId from VnPay TxnRef: {TxnRef}", txnRef);
                    TempData["Error"] = "Lỗi xử lý đơn hàng: Không tìm thấy ID đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                var isSuccessful = response.Vnp_ResponseCode == "00";
                var transactionId = response.Vnp_TransactionNo ?? "";
                
                // Extract ALL VNPay response data for full mapping
                var bankCode = Request.Query["vnp_BankCode"].ToString();
                var bankTranNo = Request.Query["vnp_BankTranNo"].ToString();
                var cardType = Request.Query["vnp_CardType"].ToString();
                var vnpAmount = Request.Query["vnp_Amount"].ToString();
                var vnpPayDate = Request.Query["vnp_PayDate"].ToString();
                var vnpOrderInfo = response.Vnp_OrderInfo ?? "";

                if (isSuccessful)
                {
                    // Use service to complete VNPay payment with ALL data mapping to Invoice
                    await _paymentService.CompleteVnPayPaymentFullAsync(
                        localOrderId,
                        transactionId,
                        txnRef,
                        bankCode,
                        bankTranNo,
                        cardType,
                        DateTime.UtcNow);
                    
                    // Store ALL data in TempData for success page display
                    TempData["VnPay_TxnRef"] = txnRef;
                    TempData["VnPay_TransactionNo"] = transactionId;
                    TempData["VnPay_BankCode"] = bankCode;
                    TempData["VnPay_BankTranNo"] = bankTranNo;
                    TempData["VnPay_CardType"] = cardType;
                    TempData["VnPay_Amount"] = vnpAmount;
                    TempData["VnPay_PayDate"] = vnpPayDate;
                    TempData["VnPay_ResponseCode"] = response.Vnp_ResponseCode;
                    TempData["VnPay_OrderInfo"] = vnpOrderInfo;
                    
                    // Generate secure token
                    var token = GenerateSecureToken(localOrderId, transactionId);
                    
                    _logger.LogInformation(
                        "VNPay payment successful: OrderId={OrderId}, TxnRef={TxnRef}, TxnNo={TxnNo}, Bank={Bank}, BankTranNo={BankTranNo}",
                        localOrderId, txnRef, transactionId, bankCode, bankTranNo);

                    return RedirectToAction("VnPaySuccess", "Payment", new { token });
                }
                else
                {
                    // Handle payment failure via service
                    await _paymentService.HandlePaymentFailureAsync(localOrderId, $"VNPay response code: {response.Vnp_ResponseCode}");
                    
                    TempData["Warning"] = $"Thanh toán VNPay không thành công. Mã lỗi: {response.Vnp_ResponseCode}";
                    return RedirectToAction("Index", "Cart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling VNPay return");
                TempData["Error"] = "Lỗi trong quá trình xử lý kết quả thanh toán VNPay.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// VNPay success redirect page
        /// Uses secure token instead of exposing orderId
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VnPaySuccess(string? token)
        {
            try
            {
                _logger.LogInformation("VNPay success page accessed with token: {Token}", token?[..Math.Min(8, token?.Length ?? 0)] + "...");

                // Get VNPay data from TempData
                var txnRef = TempData["VnPay_TxnRef"]?.ToString();
                var transactionNo = TempData["VnPay_TransactionNo"]?.ToString();
                var bankCode = TempData["VnPay_BankCode"]?.ToString();
                var bankTranNo = TempData["VnPay_BankTranNo"]?.ToString();
                var cardType = TempData["VnPay_CardType"]?.ToString();
                var vnpAmount = TempData["VnPay_Amount"]?.ToString();
                var vnpPayDate = TempData["VnPay_PayDate"]?.ToString();
                var responseCode = TempData["VnPay_ResponseCode"]?.ToString();
                var orderInfo = TempData["VnPay_OrderInfo"]?.ToString();

                // ✅ UPDATED: Parse orderId from new TxnRef format: INV-{OrderId}-{Date}-{UniqueCode}
                int orderId = 0;
                if (!string.IsNullOrEmpty(txnRef))
                {
                    if (txnRef.StartsWith("INV-"))
                    {
                        // New format: INV-{OrderId}-{Date}-{UniqueCode}
                        var parts = txnRef.Split('-');
                        if (parts.Length >= 2)
                        {
                            int.TryParse(parts[1], out orderId);
                        }
                    }
                    else if (txnRef.Contains("_"))
                    {
                        // Legacy format: {OrderId}_{tick}
                        var orderIdStr = txnRef.Split('_').FirstOrDefault();
                        int.TryParse(orderIdStr, out orderId);
                    }
                }
                
                if (orderId <= 0)
                {
                    _logger.LogWarning("VnPaySuccess: Invalid or missing TxnRef in TempData. User may have refreshed page.");
                    TempData["Info"] = "Thanh toán đã hoàn tất. Vui lòng kiểm tra đơn hàng của bạn.";
                    return RedirectToAction("Index", "Order");
                }

                var order = await _paymentRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found on VNPay success", orderId);
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Index", "Home");
                }

                if (order.OrderDetails == null || !order.OrderDetails.Any())
                {
                    _logger.LogWarning("Order {OrderId} has no order details", orderId);
                    TempData["Error"] = "Đơn hàng không có sản phẩm.";
                    return RedirectToAction("Index", "Home");
                }

                // Build ViewModel
                var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
                var discountAmount = subtotal - order.TotalAmount;

                // ✅ Extract invoice number from TxnRef (remove unique code suffix)
                // INV-63-20260108-A1B2 -> INV-63-20260108
                var invoiceFromTxn = txnRef ?? "";
                if (invoiceFromTxn.StartsWith("INV-"))
                {
                    var parts = invoiceFromTxn.Split('-');
                    if (parts.Length >= 3)
                    {
                        invoiceFromTxn = $"{parts[0]}-{parts[1]}-{parts[2]}"; // INV-{OrderId}-{Date}
                    }
                }

                var viewModel = new VnPaySuccessViewModel
                {
                    OrderId = order.Id,
                    OrderCreatedAt = order.CreatedAt,
                    OrderStatus = order.Status,
                    TotalAmount = order.TotalAmount,
                    Subtotal = subtotal,
                    DiscountAmount = discountAmount > 0 ? discountAmount : 0,

                    TransactionId = transactionNo ?? order.Payment?.TransactionId ?? "",
                    PaymentMethod = "VNPay",
                    PaymentStatus = order.Payment?.Status ?? "Paid",
                    PaidAt = order.Payment?.PaidAt,

                    // ✅ Use clean invoice number format
                    InvoiceNumber = order.Invoice?.InvoiceNumber ?? invoiceFromTxn,
                    InvoiceIssuedAt = order.Invoice?.IssuedAt,
                    InvoiceStatus = order.Invoice?.Status ?? Models.Orders.InvoiceStatus.Paid,

                    // VNPay specific - Basic
                    VnPayTxnRef = txnRef ?? order.Invoice?.VnPayTxnRef ?? "",
                    VnPayBankCode = bankCode ?? order.Invoice?.VnPayBankCode ?? "",
                    VnPayResponseCode = responseCode ?? "00",

                    // VNPay specific - Extended mapping
                    VnPayTransactionNo = transactionNo ?? order.Invoice?.VnPayTransactionId ?? "",
                    VnPayBankTranNo = bankTranNo ?? order.Invoice?.VnPayBankTranNo ?? "",
                    VnPayCardType = cardType ?? order.Invoice?.VnPayCardType ?? "",
                    VnPayAmount = vnpAmount ?? "",
                    VnPayPayDate = vnpPayDate ?? "",
                    VnPayOrderInfo = orderInfo ?? $"Thanh toan don hang {order.Id}",

                    ShippingFullName = order.ShippingAddress?.FullName ?? "",
                    ShippingPhone = order.ShippingAddress?.PhoneNumber ?? "",
                    ShippingAddress = order.ShippingAddress?.Address ?? "",
                    ShippingDistrict = order.ShippingAddress?.District ?? "",
                    ShippingCity = order.ShippingAddress?.City ?? "",

                    SecureToken = token ?? "",

                    Items = order.OrderDetails.Select(od => new OrderItemViewModel
                    {
                        ProductName = od.ProductVariant?.Product?.Name ?? "Sản phẩm",
                        VariantColor = od.ProductVariant?.Color ?? "",
                        VariantSize = od.ProductVariant?.Size ?? "",
                        ImageUrl = od.ProductVariant?.ImageUrl ?? "/images/no-image.svg",
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice
                    }).ToList()
                };

                _logger.LogInformation(
                    "VnPaySuccess page loaded: OrderId={OrderId}, TxnRef={TxnRef}, Invoice={Invoice}, BankTranNo={BankTranNo}",
                    orderId, viewModel.VnPayTxnRef, viewModel.InvoiceNumber, viewModel.VnPayBankTranNo);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading VNPay success page");
                TempData["Info"] = "Đơn hàng của bạn đã được xử lý. Vui lòng kiểm tra danh sách đơn hàng.";
                return RedirectToAction("Index", "Order");
            }
        }
    }
}