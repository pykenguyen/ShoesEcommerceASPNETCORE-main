using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoesEcommerce.Models.ViewModels;
using System.Security.Claims;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: /tra-cuu-don-hang - Public (no auth required)
        [AllowAnonymous]
        public IActionResult Track()
        {
            return View();
        }

        // POST: /Order/TrackOrder - Public API
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrackOrder(string orderNumber, string phone)
        {
            if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(phone))
            {
                return Json(new { success = false, message = "Vui lòng nhập mã đơn hàng và số điện thoại" });
            }

            var order = await _orderService.GetOrderByOrderNumberAndPhoneAsync(orderNumber, phone);
            
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng. Vui lòng kiểm tra lại mã đơn hàng và số điện thoại." });
            }

            return Json(new { 
                success = true, 
                order = new {
                    orderNumber = order.OrderNumber,
                    createdAt = order.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    status = order.Status,
                    statusText = GetStatusText(order.Status),
                    paymentStatus = order.PaymentStatus,
                    paymentStatusText = order.PaymentStatus == "Paid" ? "Đã thanh toán" : "Chưa thanh toán",
                    totalAmount = order.TotalAmount.ToString("N0"),
                    shippingAddress = new {
                        fullName = order.ShippingAddress.FullName,
                        phone = order.ShippingAddress.PhoneNumber,
                        address = order.ShippingAddress.Address,
                        district = order.ShippingAddress.District,
                        city = order.ShippingAddress.City
                    },
                    items = order.OrderDetails.Select(od => new {
                        name = od.ProductVariant.Name,
                        color = od.ProductVariant.Color,
                        size = od.ProductVariant.Size,
                        quantity = od.Quantity,
                        price = od.UnitPrice.ToString("N0"),
                        imageUrl = od.ProductVariant.ImageUrl
                    })
                }
            });
        }

        private string GetStatusText(string status)
        {
            return status?.ToLower() switch
            {
                "pending" => "Chờ xử lý",
                "confirmed" => "Đã xác nhận",
                "processing" => "Đang xử lý",
                "shipping" => "Đang giao hàng",
                "delivered" => "Đã giao hàng",
                "completed" => "Hoàn thành",
                "cancelled" => "Đã hủy",
                _ => status ?? "Không xác định"
            };
        }

        // GET: /Order
        public async Task<IActionResult> Index()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId <= 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            var orders = await _orderService.GetCustomerOrdersAsync(customerId);
            return View(orders);
        }

        // GET: /Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId <= 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _orderService.GetOrderByIdAsync(id, customerId);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // GET: /Order/Checkout
        public async Task<IActionResult> Checkout()
        {
            var customerId = GetCurrentCustomerId();
            if (customerId <= 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            var checkoutData = await _orderService.GetCheckoutDataAsync(customerId);
            if (checkoutData.CartItems == null || !checkoutData.CartItems.Any())
            {
                TempData["Message"] = "Giỏ hàng trống, vui lòng thêm sản phẩm trước khi thanh toán";
                return RedirectToAction("Index", "Cart");
            }

            return View(checkoutData);
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CreateOrderViewModel model)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId <= 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                var checkoutData = await _orderService.GetCheckoutDataAsync(customerId);
                checkoutData.OrderInfo = model;
                return View(checkoutData);
            }

            var order = await _orderService.CreateOrderAsync(model, customerId);
            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        // GET: /Order/Success/5
        public async Task<IActionResult> Success(int id)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId <= 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            var order = await _orderService.GetOrderByIdAsync(id, customerId);
            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: /Order/CreateShippingAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShippingAddress(CreateShippingAddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            var customerId = GetCurrentCustomerId();
            if (customerId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var address = await _orderService.CreateShippingAddressAsync(model, customerId);

            return Json(new
            {
                success = true,
                message = "Thêm địa chỉ thành công",
                address = new
                {
                    id = address.Id,
                    fullName = address.FullName,
                    phoneNumber = address.PhoneNumber,
                    address = address.Address,
                    city = address.City,
                    district = address.District
                }
            });
        }

        // POST: /Order/UpdatePaymentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int orderId, string status)
        {
            var customerId = GetCurrentCustomerId();
            if (customerId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var result = await _orderService.UpdatePaymentStatusAsync(orderId, status);
            if (result)
                return Json(new { success = true, message = "Cập nhật trạng thái thanh toán thành công" });

            return Json(new { success = false, message = "Không thể cập nhật trạng thái thanh toán" });
        }

        private int GetCurrentCustomerId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var customerId))
            {
                return customerId;
            }
            return -1;
        }
    }
}
